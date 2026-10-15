using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private static EncryptedRecordCodec OpenPrivateStorageCodec()
        {
            var key = PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY");
            try { return new EncryptedRecordCodec(key,compress:true); }
            finally { CryptographicOperations.ZeroMemory(key); }
        }

        private static string PrivateStorageContext(string kind,BigInteger entityId)
            => "oasis/ethereum/storage/v1/" + kind + "/" + entityId.ToString(CultureInfo.InvariantCulture);

        private static string SerializePrivateStorage(string kind,IHolonBase record)
        {
            ArgumentNullException.ThrowIfNull(record);
            using var codec = OpenPrivateStorageCodec();
            record.ProviderUniqueStorageKey ??= new System.Collections.Generic.Dictionary<Core.Enums.ProviderType,string>();
            record.ProviderUniqueStorageKey[Core.Enums.ProviderType.EthereumOASIS] = record.Id.ToString();
            return codec.Encrypt(PrivateStorageContext(kind,HashUtility.GetNumericHash(record.Id.ToString())),JsonConvert.SerializeObject(record));
        }

        private static T DeserializePrivateStorage<T>(string kind,BigInteger entityId,string ciphertext,Guid? expectedId = null) where T : class,IHolonBase
        {
            if (string.IsNullOrEmpty(ciphertext) && entityId == 0)
                throw new System.Collections.Generic.KeyNotFoundException("No Ethereum record exists at the requested storage identity.");
            using var codec = OpenPrivateStorageCodec();
            var record = JsonConvert.DeserializeObject<T>(codec.Decrypt(PrivateStorageContext(kind,entityId),ciphertext),new StoredHolonConverter());
            if (record == null || HashUtility.GetNumericHash(record.Id.ToString()) != entityId)
                throw new InvalidDataException("Encrypted Ethereum record identity does not match its storage key.");
            if (expectedId.HasValue && record.Id != expectedId.Value)
                throw new InvalidDataException("Encrypted Ethereum record identity does not match the requested GUID; numeric storage key collision.");
            return record;
        }

        private sealed class StoredHolonConverter : Newtonsoft.Json.Converters.CustomCreationConverter<IHolon>
        {
            public override IHolon Create(Type objectType) => new Core.Holons.Holon();
            public override bool CanConvert(Type objectType) => objectType == typeof(IHolon);
        }
    }
}
