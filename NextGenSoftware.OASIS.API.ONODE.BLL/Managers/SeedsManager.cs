using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.BLL.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Managers
{
    public class SeedsManager : OASISManager, ISeedsManager
    {
        public OASISResult<SeedTransaction> SaveSeedTransaction(Guid avatarId, string avatarUserName, int amount, string memo)
        {
            return Data.SaveHolon<SeedTransaction>(new SeedTransaction()
            {
                ParentHolonId = avatarId,
                AvatarId = avatarId,
                Amount = amount,
                AvatarUserName = avatarUserName,
                Memo = memo
            });
        }

        public async Task<OASISResult<SeedTransaction>> SaveSeedTransactionAsync(Guid avatarId, string avatarUserName, int amount, string memo)
        {
            return await Data.SaveHolonAsync<SeedTransaction>(new SeedTransaction()
            {
                ParentHolonId = avatarId,
                AvatarId = avatarId,
                Amount = amount,
                AvatarUserName = avatarUserName,
                Memo = memo
            });
        }

        public OASISResult<IEnumerable<SeedTransaction>> LoadSeedTransactionsForAvatar(Guid avatarId)
        {
            return Data.LoadHolonsForParent<SeedTransaction>(avatarId);
        }

        public async Task<OASISResult<IEnumerable<SeedTransaction>>> LoadSeedTransactionsForAvatarAsync(Guid avatarId)
        {
            return await Data.LoadHolonsForParentAsync<SeedTransaction>(avatarId);
        }
    }
}