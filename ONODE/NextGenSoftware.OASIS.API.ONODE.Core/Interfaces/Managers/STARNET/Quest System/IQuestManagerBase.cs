using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface IQuestManagerBase<T1> where T1 : IQuestBase, new()
    {
        OASISResult<T1> AddQuest(Guid avatarId, Guid parentId, IQuest quest, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> AddQuestAsync(Guid avatarId, Guid parentId, IQuest quest, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> GetCurentQuest(Guid avatarId, Guid chapterId, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> GetCurentQuestAsync(Guid avatarId, Guid chapterId, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T1> RemoveQuest(Guid avatarId, Guid parentId, Guid questId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> RemoveQuestAsync(Guid avatarId, Guid parentId, Guid questId, ProviderType providerType = ProviderType.Default);
    }
}