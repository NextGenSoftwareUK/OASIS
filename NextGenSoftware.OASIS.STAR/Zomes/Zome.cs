using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.STAR.Zomes
{
    public class Zome : ZomeBase, IZome
    {
        public Zome() : base()
        {

        }

        public Zome(Guid id) : base()
        {
            this.Id = id;
        }

        public Zome(string providerKey, ProviderType providerType = ProviderType.Default) : base()
        {
            if (providerType == ProviderType.Default)
                providerType = ProviderManager.Instance.CurrentStorageProviderType.Value;

            this.ProviderUniqueStorageKey[providerType] = providerKey;
        }
    }
}
