using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class ConditionalEncryptionCapabilityTests
{
    [TestMethod]
    public void FactoriesCannotAdvertiseNativePreconditionsForAnOrdinaryBackend()
    {
        var key=RandomNumberGenerator.GetBytes(32);var reads=0;
        try {
            var plain=new InMemoryBackend();
            using var encrypted=EncryptedKeyValueBackend.Create(plain,key);
            using var hidden=PrivateKeyValueBackend.Create(plain,key);
            using var persisted=PersistedPrivateKeyValueBackend.Create(plain,()=>{reads++;return key.AsSpan().ToArray();});
            Assert.IsFalse(encrypted is IConditionalKeyValueBackend);
            Assert.IsFalse(hidden is IConditionalKeyValueBackend);
            Assert.IsFalse(persisted is IConditionalKeyValueBackend);
            Assert.AreEqual(0,reads,"Capability discovery must not resolve or replace a persisted key.");
        }finally{CryptographicOperations.ZeroMemory(key);}
    }
}
