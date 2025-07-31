using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public interface IOAPPManagerBase<T1, T2, T3, T4>
        where T1 : IOAPPBase, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, int templateVersionSequence, Guid libraryId, int libraryVersionSequnce, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddLibrary(Guid avatarId, Guid parentId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, int templateVersion, Guid libraryId, int libraryVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddLibraryAsync(Guid avatarId, Guid parentId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddOAPPTemplate(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequnce, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddOAPPTemplate(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, Guid parentId, int parentVersion, Guid templateId, int subTemplateVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddOAPPTemplateAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, int templateVersionSequence, Guid runtimeId, int runtimeVersionSequnce, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> AddRuntime(Guid avatarId, Guid parentId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, int templateVersionSequence, Guid runtimeId, int runtimeVersionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddRuntimeAsync(Guid avatarId, Guid parentId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, int templateVersionSequence, Guid libraryId, int libraryVersionSequnce, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveLibrary(Guid avatarId, Guid parentId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, int templateVersionSequence, Guid libraryId, int libraryVersionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveLibraryAsync(Guid avatarId, Guid parentId, string templateVersion, Guid libraryId, string libraryVersion, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequnce, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveOAPPTemplate(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, int parentVersionSequence, Guid templateId, int subTemplateVersionSequence, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveOAPPTemplateAsync(Guid avatarId, Guid parentId, string parentVersion, Guid templateId, string subTemplateVersion, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, int templateVersionSequence, Guid runtimeId, int runtimeVersionSequnce, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveRuntime(Guid avatarId, Guid parentId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, int templateVersion, Guid runtimeId, int runtimeVersion, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveRuntimeAsync(Guid avatarId, Guid parentId, string templateVersion, Guid runtimeId, string runtimeVersion, ProviderType providerType = ProviderType.Default);
    }
}