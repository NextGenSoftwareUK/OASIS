using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var payload=SerializeCanonicalEmbeddedRecord(record);
            return codec.Encrypt(PrivateStorageContext(kind,HashUtility.GetNumericHash(record.Id.ToString())),payload.ToString(Formatting.None));
        }

        private static void CommitEthereumStorageIdentity(IHolonBase record)
        {
            record.ProviderUniqueStorageKey ??= new System.Collections.Generic.Dictionary<Core.Enums.ProviderType,string>();
            record.ProviderUniqueStorageKey[Core.Enums.ProviderType.EthereumOASIS]=record.Id.ToString();
        }

        private static Newtonsoft.Json.Linq.JObject SerializeCanonicalEmbeddedRecord(IHolonBase record)
        {
            var payload=OasisJson.ParseStoredObject(OasisJson.Serialize(record));
            var links=record.ProviderUniqueStorageKey==null ? new System.Collections.Generic.Dictionary<Core.Enums.ProviderType,string>()
                : new System.Collections.Generic.Dictionary<Core.Enums.ProviderType,string>(record.ProviderUniqueStorageKey);
            links[Core.Enums.ProviderType.EthereumOASIS]=record.Id.ToString();
            payload[nameof(IHolonBase.ProviderUniqueStorageKey)]=Newtonsoft.Json.Linq.JObject.FromObject(links);
            // Ethereum's graph contract stores selected embedded children as well as native rows.
            // Other navigation/manager references remain excluded by the canonical serializer.
            if(record is IHolon holon && holon.Children!=null)
            {
                var children=new Newtonsoft.Json.Linq.JArray();
                foreach(var child in holon.Children) children.Add(SerializeCanonicalEmbeddedRecord(child));
                payload[nameof(IHolon.Children)]=children;
            }
            return payload;
        }

        private static T DeserializePrivateStorage<T>(string kind,BigInteger entityId,string ciphertext,Guid? expectedId = null) where T : class,IHolonBase
            => ReadPrivateStorageVersion<T>(kind,entityId,ciphertext,expectedId,0);

        private static T ReadPrivateStorageVersion<T>(string kind,BigInteger entityId,string ciphertext,Guid? expectedId,int version) where T:class,IHolonBase
        {
            if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
            var decoded=DecryptStorageDocument<T>(kind,entityId,ciphertext,expectedId);
            return version==0?decoded.Record:RecordHistory.ReadAsync(decoded.Record.Id,decoded.Document,version,OasisJson.DeserializeStored<T>).GetAwaiter().GetResult();
        }

        private static (T Record,Newtonsoft.Json.Linq.JObject Document) DecryptStorageDocument<T>(string kind,BigInteger entityId,string ciphertext,Guid? expectedId) where T:class,IHolonBase
        {
            if (string.IsNullOrEmpty(ciphertext) && entityId == 0)
                throw new System.Collections.Generic.KeyNotFoundException("No Ethereum record exists at the requested storage identity.");
            using var codec = OpenPrivateStorageCodec();
            var json=codec.Decrypt(PrivateStorageContext(kind,entityId),ciphertext);
            var record = OasisJson.DeserializeStored<T>(json);
            if (record == null || record.Id==Guid.Empty || HashUtility.GetNumericHash(record.Id.ToString()) != entityId)
                throw new InvalidDataException("Encrypted Ethereum record identity does not match its storage key.");
            if (expectedId.HasValue && record.Id != expectedId.Value)
                throw new InvalidDataException("Encrypted Ethereum record identity does not match the requested GUID; numeric storage key collision.");
            var document=OasisJson.ParseStoredObject(json);
            if(document["id"]!=null && (document["id"].Type!=Newtonsoft.Json.Linq.JTokenType.String || document["id"].Value<string>()!=record.Id.ToString()))
                throw new InvalidDataException("Ethereum embedded history item identity differs from payload.");
            document["id"]=record.Id.ToString();
            record=RecordHistory.ReadAsync(record.Id,document,0,OasisJson.DeserializeStored<T>).GetAwaiter().GetResult();
            return (record,document);
        }

        private sealed class StoredHolonConverter : Newtonsoft.Json.Converters.CustomCreationConverter<IHolon>
        {
            public override IHolon Create(Type objectType) => new Core.Holons.Holon();
            public override bool CanConvert(Type objectType) => objectType == typeof(IHolon);
        }

    }
}
