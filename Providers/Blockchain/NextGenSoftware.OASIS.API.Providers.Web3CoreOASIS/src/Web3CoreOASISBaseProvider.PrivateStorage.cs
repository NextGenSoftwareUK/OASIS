using System;
using System.IO;
using System.Globalization;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider
{
    private string PrivateContext(string kind,System.Numerics.BigInteger entityId,byte[] externalId)
    {
        if(entityId<0 || entityId>uint.MaxValue || externalId==null || externalId.Length!=32) throw new InvalidDataException("Invalid Web3Core encrypted storage identity.");
        if(!_expectedChainId.HasValue || _expectedChainId.Value<=0) throw new InvalidOperationException("Encrypted Web3Core storage requires an explicit chain identity.");
        return $"oasis/web3core/private/v2/{ProviderType.Value}/{_expectedChainId.Value.ToString(CultureInfo.InvariantCulture)}/{_contractAddress.ToLowerInvariant()}/{kind}/{entityId.ToString(CultureInfo.InvariantCulture)}/{Convert.ToHexString(externalId)}";
    }

    private static EncryptedRecordCodec OpenPrivateCodec()
    {
        var key=PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");
        try {return new EncryptedRecordCodec(key,compress:true);}
        finally {CryptographicOperations.ZeroMemory(key);}
    }

    internal string EncodePrivateRecord(string kind,IHolonBase record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if(record.Id==Guid.Empty) throw new ArgumentException("A nonempty record identity is required.",nameof(record));
        var context=PrivateContext(kind,(uint)HashUtility.GetNumericHash(record.Id.ToString()),PrivateExternalId(record.Id));
        using var codec=OpenPrivateCodec();
        return codec.Encrypt(context,OasisJson.Serialize(record));
    }

    internal T DecodePrivateRecord<T>(string kind,Guid id,EntityOASIS entity) where T:class,IHolonBase
    {
        ValidatePrivateRecordIdentity(id,entity);
        var record=DecodePrivateEntity<T>(kind,entity);
        if(record==null || record.Id!=id) throw new InvalidDataException("Web3Core private payload identity differs from requested record.");
        return record;
    }

    private static void ValidatePrivateRecordIdentity(Guid id,EntityOASIS entity)
    {
        if(entity==null || entity.EntityId!=(uint)HashUtility.GetNumericHash(id.ToString())
            || entity.ExternalId==null || !CryptographicOperations.FixedTimeEquals(entity.ExternalId,PrivateExternalId(id)))
            throw new InvalidDataException("Web3Core storage identity differs from requested record.");
    }

    internal T DecodePrivateEntity<T>(string kind,EntityOASIS entity,int version=0) where T:class,IHolonBase
    {
        if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
        var decoded=DecryptPrivateDocument<T>(kind,entity);
        return version==0?decoded.Record:PrivateHistory.ReadAsync(decoded.Record.Id,decoded.Document,version,OasisJson.Deserialize<T>).GetAwaiter().GetResult();
    }

    private (T Record,Newtonsoft.Json.Linq.JObject Document) DecryptPrivateDocument<T>(string kind,EntityOASIS entity) where T:class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(entity);
        var context=PrivateContext(kind,entity.EntityId,entity.ExternalId);
        using var codec=OpenPrivateCodec();
        var json=codec.Decrypt(context,Encoding.UTF8.GetString(entity.Info));
        var record=OasisJson.Deserialize<T>(json);
        if(record==null || record.Id==Guid.Empty || entity.EntityId!=(uint)HashUtility.GetNumericHash(record.Id.ToString())
            || !CryptographicOperations.FixedTimeEquals(entity.ExternalId,PrivateExternalId(record.Id)))
            throw new InvalidDataException("Enumerated Web3Core payload identity differs from its storage identity.");
        var document=OasisJson.ParseStoredObject(json);
        if(document["id"]!=null && document["id"].Value<string>()!=record.Id.ToString())
            throw new InvalidDataException("Web3Core embedded history identity differs from payload.");
        document["id"]=record.Id.ToString();
        record=PrivateHistory.ReadAsync(record.Id,document,0,OasisJson.Deserialize<T>).GetAwaiter().GetResult();
        return (record,document);
    }

    internal static byte[] PrivateExternalId(Guid id)=>SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("D")));
}
