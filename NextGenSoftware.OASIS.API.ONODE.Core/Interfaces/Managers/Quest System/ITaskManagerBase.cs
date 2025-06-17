using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public interface ITaskManagerBase<T1> where T1 : IQuestBase, new()
    {
        OASISResult<T1> Complete(Guid avatarId, Guid chapterId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T1>> CompleteAsync(Guid avatarId, Guid chapterId, ProviderType providerType = ProviderType.Default);
    }
}