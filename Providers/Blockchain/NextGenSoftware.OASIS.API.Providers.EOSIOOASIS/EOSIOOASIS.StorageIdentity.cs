using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public partial class EOSIOOASIS
    {
        // The contract indexes app records by their OASIS ID. Account names and legacy
        // auxiliary tables are not keys for records saved through these repositories.
        private T WithStorageIdentity<T>(T record) where T : IHolonBase
        {
            if (record.Id == Guid.Empty)
                throw new InvalidOperationException("A persisted EOSIO record requires a nonempty OASIS ID.");
            record.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
            record.ProviderUniqueStorageKey[StorageProviderType] = record.Id.ToString("D");
            return record;
        }

        private static Guid ParseStorageKey(string providerKey)
        {
            if (!Guid.TryParseExact(providerKey, "D", out var id) || id == Guid.Empty)
                throw new ArgumentException("EOSIO storage keys must be nonempty OASIS IDs in D format.", nameof(providerKey));
            return id;
        }

        private void AcknowledgeStorageIdentity(IHolonBase source, IHolonBase persisted)
        {
            source.Id = persisted.Id;
            source.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
            source.ProviderUniqueStorageKey[StorageProviderType] = persisted.Id.ToString("D");
        }
    }
}
