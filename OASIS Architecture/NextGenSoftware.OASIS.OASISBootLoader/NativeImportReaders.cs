using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader
{
    /// <summary>
    /// Readers for importing from a caller-supplied connection. They are the same provider classes the node registers, created
    /// with placeholder settings and never activated: a connection names everything the reader needs, so importing your own
    /// database does not depend on the node having that provider configured or enabled. The placeholders are never contacted.
    /// </summary>
    public static class NativeImportReaders
    {
        /// <summary>The provider types that can import from a caller-supplied connection.</summary>
        public static readonly IReadOnlyList<ProviderType> SupportedTypes = new[]
        {
            ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.AzureCosmosDBOASIS,
            ProviderType.SOLIDOASIS, ProviderType.ActivityPubOASIS, ProviderType.IPFSOASIS, ProviderType.SolanaOASIS
        };

        /// <summary>A reader for the type, or null when that provider cannot import native data.</summary>
        public static INativeDataImportProvider Create(ProviderType providerType) => providerType switch
        {
            ProviderType.MongoDBOASIS => new MongoDBOASIS("mongodb://127.0.0.1:27017", "oasis-native-reader"),
            ProviderType.Neo4jOASIS => new Neo4jOASIS("bolt://127.0.0.1:7687", "reader", "reader"),
            ProviderType.SQLLiteDBOASIS => new SQLLiteDBOASIS("Data Source=:memory:"),
            ProviderType.AzureCosmosDBOASIS => new AzureCosmosDBOASIS(new Uri("https://127.0.0.1:8081"), "unused", "unused", new List<string>()),
            ProviderType.SOLIDOASIS => new SOLIDOASIS("https://reader.invalid", ""),
            ProviderType.ActivityPubOASIS => new ActivityPubOASIS("https://reader.invalid", ""),
            ProviderType.IPFSOASIS => new IPFSOASIS(OASISDNAManager.OASISDNA ?? new OASISDNA()),
            ProviderType.SolanaOASIS => CreateSolana(),
            _ => null
        };

        private static INativeDataImportProvider CreateSolana()
        {
            var account = new Solnet.Wallet.Account();
            return new SolanaOASIS("https://reader.invalid", account.PrivateKey.Key, account.PublicKey.Key);
        }
    }
}
