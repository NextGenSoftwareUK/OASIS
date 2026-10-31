using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;
[TestClass]
public class PrivateEnvelopeIntegrityTests
{
    [DataTestMethod]
    [DataRow("{}")] [DataRow("[]")] [DataRow("null")] [DataRow("{invalid")]
    [DataRow("{\"key\":12,\"value\":\"x\"}")] [DataRow("{\"key\":\"owned\",\"value\":12}")]
    public async Task AuthenticatedButInvalidPrivatePayloadIsTerminal(string payload)
    {
        var backend=new InMemoryBackend();var key=Enumerable.Range(0,32).Select(value=>(byte)value).ToArray();
        var index=HMACSHA256.HashData(key,Encoding.UTF8.GetBytes("OASIS private storage index v1"));
        var physical="oasis/private/v1/"+Convert.ToHexString(HMACSHA256.HashData(index,Encoding.UTF8.GetBytes("owned"))).ToLowerInvariant();
        using var encrypted=new EncryptedKeyValueBackend(backend,key);await encrypted.PutAsync(physical,payload);
        using var storage=new PrivateKeyValueBackend(backend,key);
        var error=await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(()=>storage.GetAsync("owned"));
        if(payload=="{invalid")Assert.IsInstanceOfType(error.InnerException,typeof(System.Text.Json.JsonException));
        await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(()=>storage.ListKeysAsync(""));
    }
}
