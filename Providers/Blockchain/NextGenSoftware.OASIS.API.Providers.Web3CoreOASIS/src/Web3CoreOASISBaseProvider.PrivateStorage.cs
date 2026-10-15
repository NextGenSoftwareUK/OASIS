using System;
using System.IO;
using System.Globalization;
using System.Text;
using System.Security.Cryptography;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider
{
    private string PrivateContext(string kind,Guid id)
    {
        if(id==Guid.Empty) throw new ArgumentException("A nonempty Web3Core record identity is required.",nameof(id));
        if(!_expectedChainId.HasValue || _expectedChainId.Value<=0) throw new InvalidOperationException("Encrypted Web3Core storage requires an explicit chain identity.");
        return $"oasis/web3core/private/v1/{ProviderType.Value}/{_expectedChainId.Value.ToString(CultureInfo.InvariantCulture)}/{_contractAddress.ToLowerInvariant()}/{kind}/{id:D}";
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
        var context=PrivateContext(kind,record.Id);
        using var codec=OpenPrivateCodec();
        return codec.Encrypt(context,OasisJson.Serialize(record));
    }

    internal T DecodePrivateRecord<T>(string kind,Guid id,EntityOASIS entity) where T:class,IHolonBase
    {
        var context=PrivateContext(kind,id);
        if(entity==null || entity.EntityId!=(uint)HashUtility.GetNumericHash(id.ToString())
            || entity.ExternalId==null || !CryptographicOperations.FixedTimeEquals(entity.ExternalId,PrivateExternalId(id)))
            throw new InvalidDataException("Web3Core storage identity differs from requested record.");
        using var codec=OpenPrivateCodec();
        var record=OasisJson.Deserialize<T>(codec.Decrypt(context,Encoding.UTF8.GetString(entity.Info)));
        if(record==null || record.Id!=id) throw new InvalidDataException("Web3Core private payload identity differs from requested record.");
        return record;
    }

    internal static byte[] PrivateExternalId(Guid id)=>SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("D")));
}
