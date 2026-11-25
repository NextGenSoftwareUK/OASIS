using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.DruidOASIS
{
    /// <summary>
    /// OASIS provider for Apache Druid. Druid is append-only, so canonical OASIS records are rows in one datasource
    /// (default "oasis_kv") where the newest row per key wins and deletes append a tombstone row
    /// (<see cref="DruidKeyValueBackend"/>). Storage semantics come from the shared <see cref="KeyValueStorageProviderBase"/>.
    /// </summary>
    public class DruidOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        public DruidOASIS(string baseUrl = "http://localhost:8888", string datasource = "oasis_kv")
            : base(new DruidKeyValueBackend(baseUrl, datasource))
        {
            ProviderName = "DruidOASIS";
            ProviderDescription = "Apache Druid real-time analytics storage provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.DruidOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        public string GetProviderVersion() => "1.0.0";
    }
}
