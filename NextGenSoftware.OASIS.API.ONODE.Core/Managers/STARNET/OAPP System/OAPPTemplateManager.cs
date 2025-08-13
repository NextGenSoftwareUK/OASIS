using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class OAPPTemplateManager : STARNETManagerBase<OAPPTemplate, DownloadedOAPPTemplate, InstalledOAPPTemplate, OAPPTemplateDNA>
    public class OAPPTemplateManager : STARNETManagerBase<OAPPTemplate, DownloadedOAPPTemplate, InstalledOAPPTemplate, STARNETDNA>
    {
        public OAPPTemplateManager(Guid avatarId, OASISDNA OASISDNA = null) : base(
            avatarId, 
            OASISDNA,
            typeof(OAPPTemplateType),
            HolonType.OAPPTemplate, 
            HolonType.InstalledOAPPTemplate, 
            "OAPP Template", 
            "OAPPTemplateId", 
            "OAPPTemplateName", 
            "OAPPTemplateType", 
            "oapptemplate", 
            "oasis_oapptemplates", 
            "OAPPTemplateDNA.json",
            "OAPPTemplateDNAJSON") { }

        public OAPPTemplateManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(
            OASISStorageProvider, 
            avatarId,
            OASISDNA,
            typeof(OAPPTemplateType),
            HolonType.OAPPTemplate, 
            HolonType.InstalledOAPPTemplate,
            "OAPP Template",
            "OAPPTemplateId",
            "OAPPTemplateName",
            "OAPPTemplateType",
            "oapptemplate",
            "oasis_oapptemplates",
            "OAPPTemplateDNA.json",
            "OAPPTemplateDNAJSON")
        { }

        //public async Task<OASISResult<IOAPPTemplate>> AddRuntimeToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, int templateVersion, Guid runtimeId, int runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddRuntimeToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync(avatarId, parentOAPPTemplateId, templateVersion, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                //List<STARNETHolonMetaData> metaData = new List<STARNETHolonMetaData>();

        //                //if (parentOAPPTemplateResult.Result.STARNETDNA.MetaData != null && parentOAPPTemplateResult.Result.STARNETDNA.MetaData.ContainsKey("RuntimesMetaData") && parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] != null)
        //                //    metaData = parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] as List<STARNETHolonMetaData>;

        //                //metaData.Add(new STARNETHolonMetaData() 
        //                //{ 
        //                //    HolonId = runtimeResult.Result.Id, 
        //                //    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                //    Name = runtimeResult.Result.Name, 
        //                //    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                //    Version = runtimeResult.Result.STARNETDNA.Version
        //                //});

        //                parentOAPPTemplateResult.Result.Runtimes.Add(runtimeResult.Result);
        //                parentOAPPTemplateResult.Result.RuntimesMetaData.Add(new STARNETHolonMetaData()
        //                {
        //                    HolonId = runtimeResult.Result.Id,
        //                    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                    Name = runtimeResult.Result.Name,
        //                    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                    Version = runtimeResult.Result.STARNETDNA.Version
        //                });

        //                //parentOAPPTemplateResult.Result.RuntimeIds.Add(runtimeResult.Result.Id.ToString());
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Runtimes"] = parentOAPPTemplateResult.Result.Runtimes;
        //                //parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentOAPPTemplateResult.Result.RuntimeIds;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentOAPPTemplateResult.Result.RuntimesMetaData;
                        
        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> AddRuntimeToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, int templateVersionSequence, Guid runtimeId, int runtimeVersionSequnce, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddRuntimeToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, templateVersionSequence, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersionSequnce.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Runtimes.Add(runtimeResult.Result);
        //                parentOAPPTemplateResult.Result.RuntimesMetaData.Add(new STARNETHolonMetaData()
        //                {
        //                    HolonId = runtimeResult.Result.Id,
        //                    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                    Name = runtimeResult.Result.Name,
        //                    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                    Version = runtimeResult.Result.STARNETDNA.Version
        //                });

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Runtimes"] = parentOAPPTemplateResult.Result.Runtimes;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentOAPPTemplateResult.Result.RuntimesMetaData;

        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IOAPPTemplate>> AddRuntimeToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddRuntimeToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await Data.LoadHolonByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
        //        {
        //            { "OAPPTemplateId", runtimeId.ToString() },
        //            { "Version", runtimeVersion }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Runtimes.Add(runtimeResult.Result);
        //                parentOAPPTemplateResult.Result.RuntimesMetaData.Add(new STARNETHolonMetaData()
        //                {
        //                    HolonId = runtimeResult.Result.Id,
        //                    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                    Name = runtimeResult.Result.Name,
        //                    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                    Version = runtimeResult.Result.STARNETDNA.Version
        //                });

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Runtimes"] = parentOAPPTemplateResult.Result.Runtimes;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentOAPPTemplateResult.Result.RuntimesMetaData;

        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> AddRuntimeToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddRuntimeToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
        //        {
        //            { "OAPPTemplateId", runtimeId.ToString() },
        //            { "Version", runtimeVersion }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Runtimes.Add(runtimeResult.Result);
        //                parentOAPPTemplateResult.Result.RuntimesMetaData.Add(new STARNETHolonMetaData()
        //                {
        //                    HolonId = runtimeResult.Result.Id,
        //                    STARNETHolonId = runtimeResult.Result.STARNETDNA.Id,
        //                    Name = runtimeResult.Result.Name,
        //                    VersionSequence = runtimeResult.Result.STARNETDNA.VersionSequence,
        //                    Version = runtimeResult.Result.STARNETDNA.Version
        //                });

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Runtimes"] = parentOAPPTemplateResult.Result.Runtimes;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentOAPPTemplateResult.Result.RuntimesMetaData;

        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}


        //public async Task<OASISResult<IOAPPTemplate>> RemoveRuntimeToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, int templateVersion, Guid runtimeId, int runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveRuntimeToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync(avatarId, parentOAPPTemplateId, templateVersion, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Runtimes.Remove(runtimeResult.Result);
        //                ISTARNETDependency metaData = parentOAPPTemplateResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (metaData != null)
        //                    parentOAPPTemplateResult.Result.RuntimesMetaData.Remove(metaData);

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Runtimes"] = parentOAPPTemplateResult.Result.Runtimes;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentOAPPTemplateResult.Result.RuntimesMetaData;

        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> RemoveRuntimeToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, int templateVersionSequence, Guid runtimeId, int runtimeVersionSequnce, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveRuntimeToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, templateVersionSequence, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersionSequnce.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Runtimes.Remove(runtimeResult.Result);
        //                ISTARNETDependency metaData = parentOAPPTemplateResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (metaData != null)
        //                    parentOAPPTemplateResult.Result.RuntimesMetaData.Remove(metaData);

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Runtimes"] = parentOAPPTemplateResult.Result.Runtimes;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentOAPPTemplateResult.Result.RuntimesMetaData;
        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IOAPPTemplate>> RemoveRuntimeToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveRuntimeToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await Data.LoadHolonByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
        //        {
        //            { "OAPPTemplateId", runtimeId.ToString() },
        //            { "Version", runtimeVersion }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonByMetaDataAsync<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Runtimes.Remove(runtimeResult.Result);
        //                ISTARNETDependency metaData = parentOAPPTemplateResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (metaData != null)
        //                    parentOAPPTemplateResult.Result.RuntimesMetaData.Remove(metaData);

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Runtimes"] = parentOAPPTemplateResult.Result.Runtimes;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentOAPPTemplateResult.Result.RuntimesMetaData;

        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaDataAsync. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> RemoveRuntimeToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveRuntimeToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
        //        {
        //            { "OAPPTemplateId", runtimeId.ToString() },
        //            { "Version", runtimeVersion }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolonByMetaData<Runtime>(new Dictionary<string, string>()
        //            {
        //                { "RuntimeId", runtimeId.ToString() },
        //                { "VersionSequence", runtimeVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Runtime, providerType: providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Runtimes.Remove(runtimeResult.Result);
        //                ISTARNETDependency metaData = parentOAPPTemplateResult.Result.RuntimesMetaData.FirstOrDefault(x => x.HolonId == runtimeResult.Result.Id);

        //                if (metaData != null)
        //                    parentOAPPTemplateResult.Result.RuntimesMetaData.Remove(metaData);

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Runtimes"] = parentOAPPTemplateResult.Result.Runtimes;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimesMetaData"] = parentOAPPTemplateResult.Result.RuntimesMetaData;

        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonByMetaData. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}



        //public async Task<OASISResult<IOAPPTemplate>> AddLibraryToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, int templateVersion, Guid libraryId, int libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddLibraryToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync(avatarId, parentOAPPTemplateId, templateVersion, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Add(libraryResult.Result);
        //                parentOAPPTemplateResult.Result.LibrariesMetaData.Add(new STARNETHolonMetaData()
        //                {
        //                    HolonId = libraryResult.Result.Id,
        //                    STARNETHolonId = libraryResult.Result.STARNETDNA.Id,
        //                    Name = libraryResult.Result.Name,
        //                    VersionSequence = libraryResult.Result.STARNETDNA.VersionSequence,
        //                    Version = libraryResult.Result.STARNETDNA.Version
        //                });

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Libs"] = parentOAPPTemplateResult.Result.Libraries;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentOAPPTemplateResult.Result.LibrariesMetaData;

        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaDataAsync. Reason: {libraryResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> AddLibraryToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, int templateVersionSequence, Guid libraryId, int libraryVersionSequnce, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddLibraryToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, templateVersionSequence, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersionSequnce.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Add(libraryResult.Result);
        //                parentOAPPTemplateResult.Result.LibrariesMetaData.Add(new STARNETHolonMetaData()
        //                {
        //                    HolonId = libraryResult.Result.Id,
        //                    STARNETHolonId = libraryResult.Result.STARNETDNA.Id,
        //                    Name = libraryResult.Result.Name,
        //                    VersionSequence = libraryResult.Result.STARNETDNA.VersionSequence,
        //                    Version = libraryResult.Result.STARNETDNA.Version
        //                });

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Libs"] = parentOAPPTemplateResult.Result.Libraries;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentOAPPTemplateResult.Result.LibrariesMetaData;

        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaData. Reason: {libraryResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IOAPPTemplate>> AddLibraryToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddLibraryToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await Data.LoadHolonByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
        //        {
        //            { "OAPPTemplateId", libraryId.ToString() },
        //            { "Version", libraryVersion }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Add(libraryResult.Result);
        //                parentOAPPTemplateResult.Result.LibrariesMetaData.Add(new STARNETHolonMetaData()
        //                {
        //                    HolonId = libraryResult.Result.Id,
        //                    STARNETHolonId = libraryResult.Result.STARNETDNA.Id,
        //                    Name = libraryResult.Result.Name,
        //                    VersionSequence = libraryResult.Result.STARNETDNA.VersionSequence,
        //                    Version = libraryResult.Result.STARNETDNA.Version
        //                });

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Libs"] = parentOAPPTemplateResult.Result.Libraries;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentOAPPTemplateResult.Result.LibrariesMetaData;

        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaDataAsync. Reason: {libraryResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> AddLibraryToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddLibraryToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
        //        {
        //            { "OAPPTemplateId", libraryId.ToString() },
        //            { "Version", libraryVersion }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Add(libraryResult.Result);
        //                parentOAPPTemplateResult.Result.LibrariesMetaData.Add(new STARNETHolonMetaData()
        //                {
        //                    HolonId = libraryResult.Result.Id,
        //                    STARNETHolonId = libraryResult.Result.STARNETDNA.Id,
        //                    Name = libraryResult.Result.Name,
        //                    VersionSequence = libraryResult.Result.STARNETDNA.VersionSequence,
        //                    Version = libraryResult.Result.STARNETDNA.Version
        //                });

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Libs"] = parentOAPPTemplateResult.Result.Libraries;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentOAPPTemplateResult.Result.LibrariesMetaData;

        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaData. Reason: {libraryResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}


        //public async Task<OASISResult<IOAPPTemplate>> RemoveLibraryToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, int templateVersion, Guid libraryId, int libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveLibraryToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync(avatarId, parentOAPPTemplateId, templateVersion, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Remove(libraryResult.Result);
        //                ISTARNETDependency metaData = parentOAPPTemplateResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libraryResult.Result.Id);

        //                if (metaData != null)
        //                    parentOAPPTemplateResult.Result.LibrariesMetaData.Remove(metaData);

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Libs"] = parentOAPPTemplateResult.Result.Libraries;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentOAPPTemplateResult.Result.LibrariesMetaData;

        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaDataAsync. Reason: {libraryResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> RemoveLibraryToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, int templateVersionSequence, Guid libraryId, int libraryVersionSequnce, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveLibraryToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, templateVersionSequence, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersionSequnce.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Remove(libraryResult.Result);
        //                ISTARNETDependency metaData = parentOAPPTemplateResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libraryResult.Result.Id);

        //                if (metaData != null)
        //                    parentOAPPTemplateResult.Result.LibrariesMetaData.Remove(metaData);

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Libs"] = parentOAPPTemplateResult.Result.Libraries;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentOAPPTemplateResult.Result.LibrariesMetaData;
        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaData. Reason: {libraryResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IOAPPTemplate>> RemoveLibraryToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveLibraryToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await Data.LoadHolonByMetaDataAsync<OAPPTemplate>(new Dictionary<string, string>()
        //        {
        //            { "OAPPTemplateId", libraryId.ToString() },
        //            { "Version", libraryVersion }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = await Data.LoadHolonByMetaDataAsync<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Remove(libraryResult.Result);
        //                ISTARNETDependency metaData = parentOAPPTemplateResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libraryResult.Result.Id);

        //                if (metaData != null)
        //                    parentOAPPTemplateResult.Result.LibrariesMetaData.Remove(metaData);

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Libs"] = parentOAPPTemplateResult.Result.Libraries;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentOAPPTemplateResult.Result.LibrariesMetaData;

        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaDataAsync. Reason: {libraryResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> RemoveLibraryToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveLibraryToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Data.LoadHolonByMetaData<OAPPTemplate>(new Dictionary<string, string>()
        //        {
        //            { "OAPPTemplateId", libraryId.ToString() },
        //            { "Version", libraryVersion }

        //        }, MetaKeyValuePairMatchMode.All, HolonType.OAPPTemplate, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = Data.LoadHolonByMetaData<Library>(new Dictionary<string, string>()
        //            {
        //                { "LibraryId", libraryId.ToString() },
        //                { "VersionSequence", libraryVersion.ToString() }

        //            }, MetaKeyValuePairMatchMode.All, HolonType.Library, providerType: providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Remove(libraryResult.Result);
        //                ISTARNETDependency metaData = parentOAPPTemplateResult.Result.LibrariesMetaData.FirstOrDefault(x => x.HolonId == libraryResult.Result.Id);

        //                if (metaData != null)
        //                    parentOAPPTemplateResult.Result.LibrariesMetaData.Remove(metaData);

        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["Libs"] = parentOAPPTemplateResult.Result.Libraries;
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibsMetaData"] = parentOAPPTemplateResult.Result.LibrariesMetaData;

        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonByMetaData. Reason: {libraryResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}










        //public async Task<OASISResult<IOAPPTemplate>> AddRuntimeToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddRuntimeToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync(avatarId, parentOAPPTemplateId, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = await Data.LoadHolonAsync<Runtime>(runtimeId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Runtimes.Add(runtimeResult.Result);
        //                parentOAPPTemplateResult.Result.RuntimeIds.Add(runtimeResult.Result.Id.ToString());
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentOAPPTemplateResult.Result.RuntimeIds;
        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonAsync. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> AddRuntimeToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddRuntimeToOAPPTemplate. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Runtime> runtimeResult = Data.LoadHolon<Runtime>(runtimeId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Runtimes.Add(runtimeResult.Result);
        //                parentOAPPTemplateResult.Result.RuntimeIds.Add(runtimeResult.Result.Id.ToString());
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentOAPPTemplateResult.Result.RuntimeIds;
        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolon. Reason: {runtimeResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IOAPPTemplate>> RemoveRuntimeFromOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveRuntimeFromOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync(avatarId, parentOAPPTemplateId, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            IRuntime runtime = parentOAPPTemplateResult.Result.Runtimes.FirstOrDefault(x => x.Id == runtimeId);

        //            if (runtime != null)
        //            {
        //                parentOAPPTemplateResult.Result.Runtimes.Remove(runtime);
        //                parentOAPPTemplateResult.Result.RuntimeIds.Remove(runtime.ToString());
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentOAPPTemplateResult.Result.RuntimeIds;
        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Runtime could be found for the id {runtimeId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> RemoveRuntimeFromOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveRuntimeFromOAPPTemplate. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            IRuntime runtime = parentOAPPTemplateResult.Result.Runtimes.FirstOrDefault(x => x.Id == runtimeId);

        //            if (runtime != null)
        //            {
        //                parentOAPPTemplateResult.Result.Runtimes.Remove(runtime);
        //                parentOAPPTemplateResult.Result.RuntimeIds.Remove(runtime.ToString());
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentOAPPTemplateResult.Result.RuntimeIds;
        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Runtime could be found for the id {runtimeId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IOAPPTemplate>> AddLibraryToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddLibraryToOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync(avatarId, parentOAPPTemplateId, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = await Data.LoadHolonAsync<Library>(libraryId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Add(libraryResult.Result);
        //                parentOAPPTemplateResult.Result.LibraryIds.Add(libraryResult.Result.Id.ToString());
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentOAPPTemplateResult.Result.LibraryIds;
        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonAsync. Reason: {libraryResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> AddLibraryToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.AddLibraryToOAPPTemplate. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            OASISResult<Library> libraryResult = Data.LoadHolon<Library>(libraryId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Add(libraryResult.Result);
        //                parentOAPPTemplateResult.Result.LibraryIds.Add(libraryResult.Result.Id.ToString());
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentOAPPTemplateResult.Result.LibraryIds;
        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolon. Reason: {libraryResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IOAPPTemplate>> RemoveLibraryFromOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveLibraryFromOAPPTemplateAsync. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync(avatarId, parentOAPPTemplateId, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            ILibrary library = parentOAPPTemplateResult.Result.Libraries.FirstOrDefault(x => x.Id == libraryId);

        //            if (library != null)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Remove(library);
        //                parentOAPPTemplateResult.Result.LibraryIds.Remove(library.ToString());
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentOAPPTemplateResult.Result.LibraryIds;
        //                result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Library could be found for the id {libraryId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPPTemplate> RemoveLibraryFromOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
        //    string errorMessage = "Error occured in OAPPTemplateManager.RemoveLibraryFromOAPPTemplate. Reason:";

        //    try
        //    {
        //        OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, providerType: providerType);

        //        if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
        //        {
        //            ILibrary library = parentOAPPTemplateResult.Result.Libraries.FirstOrDefault(x => x.Id == libraryId);

        //            if (library != null)
        //            {
        //                parentOAPPTemplateResult.Result.Libraries.Remove(library);
        //                parentOAPPTemplateResult.Result.LibraryIds.Remove(library.ToString());
        //                parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentOAPPTemplateResult.Result.LibraryIds;
        //                result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Library could be found for the id {libraryId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        private OASISResult<IOAPPTemplate> UpdateOAPPTemplate(Guid avatarId, IOAPPTemplate quest, OASISResult<IOAPPTemplate> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<OAPPTemplate> questResult = Update(avatarId, (OAPPTemplate)quest, providerType: providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = (IOAPPTemplate)questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with OAPPTemplateManager.Update. Reason: {questResult.Message}");

            return result;
        }

        private async Task<OASISResult<IOAPPTemplate>> UpdateOAPPTemplateAsync(Guid avatarId, IOAPPTemplate quest, OASISResult<IOAPPTemplate> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<OAPPTemplate> questResult = await UpdateAsync(avatarId, (OAPPTemplate)quest, providerType: providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = (IOAPPTemplate)questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with OAPPTemplateManager.Update. Reason: {questResult.Message}");

            return result;
        }
    }
}