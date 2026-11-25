using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.PinotOASIS
{
    /// <summary>
    /// OASIS provider for Apache Pinot. Pinot OFFLINE tables are append-only, so canonical OASIS records are rows in one
    /// table (default "oasis_kv", created on activation) where the newest row per key wins and deletes append a tombstone
    /// row (<see cref="PinotKeyValueBackend"/>). Storage semantics come from the shared <see cref="KeyValueStorageProviderBase"/>.
    /// </summary>
    public class PinotOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        public PinotOASIS(string brokerUrl = "http://localhost:8000", string controllerUrl = "http://localhost:9000", string table = "oasis_kv")
            : base(new PinotKeyValueBackend(brokerUrl, controllerUrl, table))
        {
            ProviderName = "PinotOASIS";
            ProviderDescription = "Apache Pinot real-time analytics storage provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PinotOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        public string GetProviderVersion() => "1.0.0";
    }
}
