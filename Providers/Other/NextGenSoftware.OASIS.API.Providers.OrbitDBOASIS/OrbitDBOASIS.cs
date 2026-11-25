using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.OrbitDBOASIS
{
    /// <summary>
    /// OASIS provider for OrbitDB, the serverless peer-to-peer database on IPFS.
    ///
    /// OrbitDB is a JavaScript library with no network API, so storage goes through the repository's OrbitDB storage
    /// service (<c>StorageService/storage-service.mjs</c>: @orbitdb/core 4 on Helia, one indexed keyvalue database,
    /// loopback HTTP with a bearer token). Every OASIS record key is one OrbitDB key; values are compressed and encrypted
    /// with the configured key because OrbitDB oplog entries replicate to any peer of the database. A write succeeds only
    /// once the service reports the oplog entry hash.
    ///
    /// Setup: <c>npm ci</c> in StorageService, then
    /// <c>OASIS_ORBITDB_STORAGE_TOKEN=... node storage-service.mjs &lt;data dir&gt; &lt;port&gt;</c>.
    /// </summary>
    public class OrbitDBOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        /// <param name="serviceUrl">OrbitDB storage service URL, e.g. <c>http://127.0.0.1:18900</c>.</param>
        /// <param name="serviceToken">The service's bearer token.</param>
        /// <param name="encryptionKey">32-byte record encryption key.</param>
        public OrbitDBOASIS(string serviceUrl, string serviceToken, byte[] encryptionKey)
            : base(CreateBackend(serviceUrl, serviceToken, encryptionKey))
        {
            ProviderName = "OrbitDBOASIS";
            ProviderDescription = "OrbitDB provider: encrypted records in an OrbitDB keyvalue database on Helia/IPFS.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.OrbitDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static IKeyValueBackend CreateBackend(string serviceUrl, string serviceToken, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("OrbitDB storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            return new PrivateKeyValueBackend(new OrbitDbServiceKeyValueBackend(serviceUrl, serviceToken), encryptionKey, compress: true);
        }
    }
}
