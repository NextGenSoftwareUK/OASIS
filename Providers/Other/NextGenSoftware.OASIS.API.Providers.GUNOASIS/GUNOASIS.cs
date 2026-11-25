using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.GUNOASIS
{
    /// <summary>
    /// OASIS provider for GUN, the decentralised offline-first graph database.
    ///
    /// Storage speaks the GUN wire protocol (DAM messages over the relay WebSocket at <c>{relay}/gun</c>); a GUN relay has
    /// no REST read/write API. Every OASIS record key is one GUN node whose <c>v</c> field holds the record, compressed and
    /// encrypted with the configured key because any GUN peer can read relay data. A delete writes <c>v = null</c>, GUN's
    /// durable tombstone. Each key directory keeps an index node (field = key, value 1 or null) that GUN merges per field,
    /// so concurrent index updates need no locking. A write succeeds only when the relay acknowledges it.
    ///
    /// Setup: <c>npm install gun</c> and run a relay with radisk persistence, e.g.
    /// <c>Gun({ web: http.createServer(Gun.serve(__dirname)).listen(8765), file: "radata" })</c>.
    /// </summary>
    public class GUNOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        /// <param name="relayUrl">GUN relay base URL, e.g. <c>http://127.0.0.1:8765</c> or <c>wss://relay.example</c>.</param>
        /// <param name="encryptionKey">32-byte record encryption key.</param>
        public GUNOASIS(string relayUrl, byte[] encryptionKey) : base(CreateBackend(relayUrl, encryptionKey))
        {
            ProviderName = "GUNOASIS";
            ProviderDescription = "GUN provider: encrypted records on a GUN relay over the GUN wire protocol (WebSocket).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.GUNOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static IKeyValueBackend CreateBackend(string relayUrl, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("GUN storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            return new PrivateKeyValueBackend(new GunRelayKeyValueBackend(GunRelayKeyValueBackend.SocketUri(relayUrl)), encryptionKey, compress: true);
        }
    }
}
