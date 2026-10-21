using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
public partial class HolonRepository
{
    private static void ValidateSaveLifecycle(IEnumerable<HolonModel> proposals,IReadOnlyDictionary<string,HolonModel> observed)
    {
        foreach(var proposal in proposals)
            if(observed.TryGetValue(proposal.Id,out var previous))
                HolonSaveLifecycle.RequireCompatibleSave(previous.DeletedDate,previous.DeletedByAvatarId,proposal.IsActive,proposal.DeletedDate,proposal.DeletedByAvatarId);
    }
}
