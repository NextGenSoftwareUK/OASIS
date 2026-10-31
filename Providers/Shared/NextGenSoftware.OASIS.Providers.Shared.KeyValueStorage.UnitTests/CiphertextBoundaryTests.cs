using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;
[TestClass]
public class CiphertextBoundaryTests
{
    [DataTestMethod]
    [DataRow("format")] [DataRow("base64")] [DataRow("short")] [DataRow("authentication")]
    public async Task InvalidStoredCiphertextIsTerminalWithOriginalCause(string mode)
    {
        var backend=new InMemoryBackend();backend.Items["owned"]=mode switch {
            "format"=>"plaintext",
            "base64"=>"oasis-aes256gcm-v1:not-base64!",
            "short"=>"oasis-aes256gcm-v1:AA==",
            _=>"oasis-aes256gcm-v1:"+Convert.ToBase64String(new byte[28])};
        using var storage=new EncryptedKeyValueBackend(backend,new byte[32]);
        var failure=await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(()=>storage.GetAsync("owned"));
        Assert.IsTrue(mode=="base64" ? failure.InnerException is FormatException:failure.InnerException is System.Security.Cryptography.CryptographicException);
    }
}
