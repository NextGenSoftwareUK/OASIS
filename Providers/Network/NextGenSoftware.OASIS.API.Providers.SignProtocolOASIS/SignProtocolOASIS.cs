using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SignProtocolOASIS
{
    /// <summary>
    /// Sign Protocol OASIS storage provider.
    ///
    /// Avatars, avatar details and holons are stored by <see cref="KeyValueStorageProviderBase"/> as attestations of one
    /// wallet-owned Sign Protocol schema (see <see cref="SignProtocolKeyValueBackend"/>): a save attests the new record and a
    /// delete revokes it. Attestation data is public on chain, so every key is an HMAC index and every value is compressed
    /// and encrypted with the configured 32-byte record key. Writes complete once their transaction is mined successfully.
    /// </summary>
    public class SignProtocolOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly SignProtocolKeyValueBackend _attestations;

        /// <param name="connection">Chain, SP contract, wallet and storage schema settings.</param>
        /// <param name="encryptionKey">32-byte record encryption key.</param>
        public SignProtocolOASIS(SignProtocolConnection connection, byte[] encryptionKey)
            : this(new SignProtocolKeyValueBackend(connection), encryptionKey) { }

        private SignProtocolOASIS(SignProtocolKeyValueBackend attestations, byte[] encryptionKey)
            : base(CreateBackend(attestations, encryptionKey))
        {
            _attestations = attestations;
            ProviderName = "SignProtocolOASIS";
            ProviderDescription = "Sign Protocol provider - encrypted key-value records as attestations of a wallet-owned schema";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SignProtocolOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        /// <summary>Address of the wallet that owns the schema and signs every attestation.</summary>
        public string WalletAddress => _attestations.WalletAddress;

        /// <summary>Sign Protocol schema id holding this provider's records.</summary>
        public ulong SchemaId => _attestations.SchemaId;

        private static IKeyValueBackend CreateBackend(SignProtocolKeyValueBackend attestations, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Sign Protocol storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            return new PrivateKeyValueBackend(attestations, encryptionKey, compress: true);
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        // ── IOASISNETProvider ─────────────────────────────────────────────────
        // Attestations carry no location, so proximity queries have no data source.

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long lat, long lon, int radius)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "SignProtocolOASIS does not support proximity queries: attestations carry no location.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long lat, long lon, int radius, HolonType holonType = HolonType.All)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "SignProtocolOASIS does not support proximity queries: attestations carry no location.");
            return result;
        }
    }
}
