using System;
using System.Security.Cryptography;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public partial class IPFSOASIS
{
    private const string PrivateIndexContext="oasis/ipfs/private-index/v1";
    private Func<byte[]> _storageKeyResolver=null;

    private EncryptedRecordCodec OpenPrivateCodec()
    {
        var key=_storageKeyResolver != null ? _storageKeyResolver()
            :PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_IPFS_STORAGE_ENCRYPTION_KEY");
        try {return new EncryptedRecordCodec(key,compress:true);}
        finally {if(key != null) CryptographicOperations.ZeroMemory(key);}
    }

    private string ProtectIndex(string json)
    {
        using var codec=OpenPrivateCodec();
        return codec.Encrypt(PrivateIndexContext,json);
    }

    private string UnprotectIndex(string envelope)
    {
        using var codec=OpenPrivateCodec();
        return codec.Decrypt(PrivateIndexContext,envelope);
    }
}
