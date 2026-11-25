using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.TablelandOASIS
{
    /// <summary>
    /// OASIS storage provider for Tableland (SQL tables governed by an EVM registry contract).
    ///
    /// Avatars, avatar details and holons are stored by <see cref="KeyValueStorageProviderBase"/> as rows of one key-value
    /// table owned by the configured wallet (see <see cref="TablelandKeyValueBackend"/>). Tableland data is public, so every
    /// key is an HMAC index and every value is compressed and encrypted with the configured 32-byte record key.
    /// Writes are signed registry transactions and complete once the validator has materialised them.
    /// </summary>
    public class TablelandOASIS : KeyValueStorageProviderBase, IOASISStorageProvider
    {
        private readonly TablelandKeyValueBackend _table;

        /// <param name="connection">Chain, validator, registry and wallet settings.</param>
        /// <param name="tableName">Existing key-value table owned by the wallet (create one with <see cref="TablelandKeyValueBackend.CreateTableAsync"/>).</param>
        /// <param name="encryptionKey">32-byte record encryption key.</param>
        public TablelandOASIS(TablelandConnection connection, string tableName, byte[] encryptionKey)
            : this(new TablelandKeyValueBackend(connection, tableName), encryptionKey) { }

        private TablelandOASIS(TablelandKeyValueBackend table, byte[] encryptionKey)
            : base(CreateBackend(table, encryptionKey))
        {
            _table = table;
            ProviderName = "TablelandOASIS";
            ProviderDescription = "Tableland provider - encrypted key-value storage in a wallet-owned Tableland table";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TablelandOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        /// <summary>Full Tableland name of the table this provider stores into.</summary>
        public string TableName => _table.TableName;

        /// <summary>Address of the wallet that owns the table and signs writes.</summary>
        public string WalletAddress => _table.WalletAddress;

        private static IKeyValueBackend CreateBackend(TablelandKeyValueBackend table, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Tableland storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            return new PrivateKeyValueBackend(table, encryptionKey, compress: true);
        }

        /// <summary>Verifies chain, table ownership and validator access, then the encrypted store round trip.</summary>
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            try
            {
                await _table.VerifyAsync();
            }
            catch (Exception ex)
            {
                IsProviderActivated = false;
                var result = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref result, $"{ProviderName} could not verify table {TableName}: {ex.Message}", ex);
                return result;
            }
            return await base.ActivateProviderAsync();
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();
    }
}
