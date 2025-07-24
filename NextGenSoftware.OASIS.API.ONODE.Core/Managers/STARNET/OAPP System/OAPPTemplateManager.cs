using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class OAPPTemplateManager : STARNETManagerBase<OAPPTemplate, DownloadedOAPPTemplate, InstalledOAPPTemplate, OAPPTemplateDNA>
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

        public async Task<OASISResult<IOAPPTemplate>> AddRuntimeToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.AddRuntimeToOAPPTemplateAsync. Reason:";

            try
            {
                OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync<OAPPTemplate>(avatarId, parentOAPPTemplateId, providerType: providerType);

                if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
                {
                    OASISResult<Runtime> runtimeResult = await Data.LoadHolonAsync<Runtime>(runtimeId, true, true, 0, true, false, HolonType.All, 0, providerType);

                    if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
                    {
                        parentOAPPTemplateResult.Result.Runtimes.Add(runtimeResult.Result);
                        parentOAPPTemplateResult.Result.RuntimeIds.Add(runtimeResult.Result.Id.ToString());
                        parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentOAPPTemplateResult.Result.RuntimeIds;
                        result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the runtime with Data.LoadHolonAsync. Reason: {runtimeResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IOAPPTemplate> AddRuntimeToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.AddRuntimeToOAPPTemplate. Reason:";

            try
            {
                OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, providerType: providerType);

                if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
                {
                    OASISResult<Runtime> runtimeResult = Data.LoadHolon<Runtime>(runtimeId, true, true, 0, true, false, HolonType.All, 0, providerType);

                    if (runtimeResult != null && runtimeResult.Result != null && !runtimeResult.IsError)
                    {
                        parentOAPPTemplateResult.Result.Runtimes.Add(runtimeResult.Result);
                        parentOAPPTemplateResult.Result.RuntimeIds.Add(runtimeResult.Result.Id.ToString());
                        parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentOAPPTemplateResult.Result.RuntimeIds;
                        result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolon. Reason: {runtimeResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> RemoveRuntimeFromOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.RemoveRuntimeFromOAPPTemplateAsync. Reason:";

            try
            {
                OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync<OAPPTemplate>(avatarId, parentOAPPTemplateId, providerType: providerType);

                if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
                {
                    IRuntime runtime = parentOAPPTemplateResult.Result.Runtimes.FirstOrDefault(x => x.Id == runtimeId);

                    if (runtime != null)
                    {
                        parentOAPPTemplateResult.Result.Runtimes.Remove(runtime);
                        parentOAPPTemplateResult.Result.RuntimeIds.Remove(runtime.ToString());
                        parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentOAPPTemplateResult.Result.RuntimeIds;
                        result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Runtime could be found for the id {runtimeId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IOAPPTemplate> RemoveRuntimeFromOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, Guid runtimeId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.RemoveRuntimeFromOAPPTemplate. Reason:";

            try
            {
                OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, providerType: providerType);

                if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
                {
                    IRuntime runtime = parentOAPPTemplateResult.Result.Runtimes.FirstOrDefault(x => x.Id == runtimeId);

                    if (runtime != null)
                    {
                        parentOAPPTemplateResult.Result.Runtimes.Remove(runtime);
                        parentOAPPTemplateResult.Result.RuntimeIds.Remove(runtime.ToString());
                        parentOAPPTemplateResult.Result.STARNETDNA.MetaData["RuntimeIds"] = parentOAPPTemplateResult.Result.RuntimeIds;
                        result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Runtime could be found for the id {runtimeId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> AddLibraryToOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.AddLibraryToOAPPTemplateAsync. Reason:";

            try
            {
                OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync<OAPPTemplate>(avatarId, parentOAPPTemplateId, providerType: providerType);

                if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
                {
                    OASISResult<Library> libraryResult = await Data.LoadHolonAsync<Library>(libraryId, true, true, 0, true, false, HolonType.All, 0, providerType);

                    if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
                    {
                        parentOAPPTemplateResult.Result.Libraries.Add(libraryResult.Result);
                        parentOAPPTemplateResult.Result.LibraryIds.Add(libraryResult.Result.Id.ToString());
                        parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentOAPPTemplateResult.Result.LibraryIds;
                        result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the library with Data.LoadHolonAsync. Reason: {libraryResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IOAPPTemplate> AddLibraryToOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.AddLibraryToOAPPTemplate. Reason:";

            try
            {
                OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, providerType: providerType);

                if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
                {
                    OASISResult<Library> libraryResult = Data.LoadHolon<Library>(libraryId, true, true, 0, true, false, HolonType.All, 0, providerType);

                    if (libraryResult != null && libraryResult.Result != null && !libraryResult.IsError)
                    {
                        parentOAPPTemplateResult.Result.Libraries.Add(libraryResult.Result);
                        parentOAPPTemplateResult.Result.LibraryIds.Add(libraryResult.Result.Id.ToString());
                        parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentOAPPTemplateResult.Result.LibraryIds;
                        result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolon. Reason: {libraryResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IOAPPTemplate>> RemoveLibraryFromOAPPTemplateAsync(Guid avatarId, Guid parentOAPPTemplateId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.RemoveLibraryFromOAPPTemplateAsync. Reason:";

            try
            {
                OASISResult<OAPPTemplate> parentOAPPTemplateResult = await LoadAsync<OAPPTemplate>(avatarId, parentOAPPTemplateId, providerType: providerType);

                if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
                {
                    ILibrary library = parentOAPPTemplateResult.Result.Libraries.FirstOrDefault(x => x.Id == libraryId);

                    if (library != null)
                    {
                        parentOAPPTemplateResult.Result.Libraries.Remove(library);
                        parentOAPPTemplateResult.Result.LibraryIds.Remove(library.ToString());
                        parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentOAPPTemplateResult.Result.LibraryIds;
                        result = await UpdateOAPPTemplateAsync(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Library could be found for the id {libraryId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplateAsync. Reason: {parentOAPPTemplateResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IOAPPTemplate> RemoveLibraryFromOAPPTemplate(Guid avatarId, Guid parentOAPPTemplateId, Guid libraryId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPTemplate> result = new OASISResult<IOAPPTemplate>();
            string errorMessage = "Error occured in OAPPTemplateManager.RemoveLibraryFromOAPPTemplate. Reason:";

            try
            {
                OASISResult<OAPPTemplate> parentOAPPTemplateResult = Load(avatarId, parentOAPPTemplateId, providerType: providerType);

                if (parentOAPPTemplateResult != null && parentOAPPTemplateResult.Result != null && !parentOAPPTemplateResult.IsError)
                {
                    ILibrary library = parentOAPPTemplateResult.Result.Libraries.FirstOrDefault(x => x.Id == libraryId);

                    if (library != null)
                    {
                        parentOAPPTemplateResult.Result.Libraries.Remove(library);
                        parentOAPPTemplateResult.Result.LibraryIds.Remove(library.ToString());
                        parentOAPPTemplateResult.Result.STARNETDNA.MetaData["LibraryIds"] = parentOAPPTemplateResult.Result.LibraryIds;
                        result = UpdateOAPPTemplate(avatarId, parentOAPPTemplateResult.Result, result, errorMessage, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No Library could be found for the id {libraryId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the OAPP Template with OAPPTemplateManager.LoadOAPPTemplate. Reason: {parentOAPPTemplateResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        private OASISResult<IOAPPTemplate> UpdateOAPPTemplate(Guid avatarId, IOAPPTemplate quest, OASISResult<IOAPPTemplate> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<OAPPTemplate> questResult = Update(avatarId, (OAPPTemplate)quest, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = (IOAPPTemplate)questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with OAPPTemplateManager.Update. Reason: {questResult.Message}");

            return result;
        }

        private async Task<OASISResult<IOAPPTemplate>> UpdateOAPPTemplateAsync(Guid avatarId, IOAPPTemplate quest, OASISResult<IOAPPTemplate> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<OAPPTemplate> questResult = await UpdateAsync(avatarId, (OAPPTemplate)quest, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = (IOAPPTemplate)questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with OAPPTemplateManager.Update. Reason: {questResult.Message}");

            return result;
        }
    }
}