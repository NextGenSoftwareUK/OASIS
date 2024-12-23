﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class SeedsManager : OASISManager, ISeedsManager
    {
        public SeedsManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public SeedsManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public OASISResult<SeedTransaction> SaveSeedTransaction(Guid avatarId, string avatarUserName, int amount, string memo)
        {
            return Data.SaveHolon<SeedTransaction>(new SeedTransaction()
            {
                ParentHolonId = avatarId,
                AvatarId = avatarId,
                Amount = amount,
                AvatarUserName = avatarUserName,
                Memo = memo
            }, AvatarId);
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
            }, AvatarId);
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