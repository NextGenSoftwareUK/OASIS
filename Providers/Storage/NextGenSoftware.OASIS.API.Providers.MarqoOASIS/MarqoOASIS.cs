using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MarqoOASIS
{
    /// <summary>
    /// OASIS provider for Marqo. Avatars, avatar details and holons are canonical OASIS records held in one Marqo index
    /// (default "oasis-kv", created without an embedding model on activation) through <see cref="MarqoKeyValueBackend"/>;
    /// storage semantics (acknowledged writes, graph saves, versions, soft and hard deletes, search) come from the shared
    /// <see cref="KeyValueStorageProviderBase"/>.
    /// </summary>
    public class MarqoOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        public MarqoOASIS(string baseUrl = "http://localhost:8882", string index = "oasis-kv")
            : base(new MarqoKeyValueBackend(baseUrl, index))
        {
            ProviderName = "MarqoOASIS";
            ProviderDescription = "Marqo tensor search engine storage provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MarqoOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        public string GetProviderVersion() => "1.0.0";
    }
}
