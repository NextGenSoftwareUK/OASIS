using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.CeramicOASIS
{
    /// <summary>
    /// OASIS provider for the Ceramic Network on a current <c>ceramic-one</c> node.
    ///
    /// ceramic-one only accepts signed events (DAG-JOSE signed by a DID, encoded as CAR files) and serves aggregated
    /// stream state; it has no key-value or document-write HTTP API. Storage goes through the repository's Ceramic storage
    /// service (<c>StorageService/storage-service.mjs</c>, the pinned Ceramic SDK), which owns one did:key and one model
    /// instance document. Every OASIS record is an entry in that document; each write or delete is one signed data event
    /// on the stream. Ceramic data replicates publicly, so values are compressed and encrypted with the configured key.
    /// ceramic-one caps an event block at 256000 bytes, so larger records are rejected explicitly.
    ///
    /// Setup: <c>npm ci</c> in StorageService, then
    /// <c>OASIS_CERAMIC_STORAGE_TOKEN=... node storage-service.mjs &lt;data dir&gt; &lt;port&gt; &lt;ceramic-one URL&gt;</c>.
    /// </summary>
    public class CeramicOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        /// <param name="serviceUrl">Ceramic storage service URL, e.g. <c>http://127.0.0.1:18931</c>.</param>
        /// <param name="serviceToken">The service's bearer token.</param>
        /// <param name="encryptionKey">32-byte record encryption key.</param>
        public CeramicOASIS(string serviceUrl, string serviceToken, byte[] encryptionKey)
            : base(CreateBackend(serviceUrl, serviceToken, encryptionKey))
        {
            ProviderName = "CeramicOASIS";
            ProviderDescription = "Ceramic provider: encrypted records in a signed Ceramic model instance document on a ceramic-one node.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CeramicOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static IKeyValueBackend CreateBackend(string serviceUrl, string serviceToken, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Ceramic storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            // Model instance document stream ids are base36 ("k...") multibase strings.
            return new PrivateKeyValueBackend(
                new StorageServiceKeyValueBackend(serviceUrl, serviceToken, "Ceramic storage service", "k"), encryptionKey, compress: true);
        }

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Geolocation is not supported.");
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Geolocation is not supported.");
            return result;
        }
    }
}
