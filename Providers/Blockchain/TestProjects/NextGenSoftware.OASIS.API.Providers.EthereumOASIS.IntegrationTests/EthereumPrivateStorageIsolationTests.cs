using System.Reflection;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class EthereumPrivateStorageIsolationTests
{
    [DataTestMethod]
    [DataRow("avatar",false)] [DataRow("avatar",true)]
    [DataRow("avatar-detail",false)] [DataRow("avatar-detail",true)]
    [DataRow("holon",false)] [DataRow("holon",true)]
    public void EncryptionPreparationDoesNotPublishProviderKeyOnCaller(string kind,bool existing)
    {
        const string keyName="OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY";
        var previous=Environment.GetEnvironmentVariable(keyName);var key=RandomNumberGenerator.GetBytes(32);
        Environment.SetEnvironmentVariable(keyName,Convert.ToBase64String(key));
        try
        {
            IHolonBase source=kind switch {"avatar"=>new Avatar(),"avatar-detail"=>new AvatarDetail(),_=>new Holon()};
            source.Id=Guid.NewGuid();source.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]="original mongo key";
            if(existing) source.ProviderUniqueStorageKey[ProviderType.EthereumOASIS]="original ethereum key";
            var original=source.ProviderUniqueStorageKey;
            var ciphertext=(string)typeof(EthereumOASIS).GetMethod("SerializePrivateStorage",BindingFlags.Static|BindingFlags.NonPublic)!
                .Invoke(null,new object[] {kind,source})!;
            Assert.AreSame(original,source.ProviderUniqueStorageKey);
            Assert.AreEqual(existing ? 2:1,source.ProviderUniqueStorageKey.Count);
            if(existing) Assert.AreEqual("original ethereum key",source.ProviderUniqueStorageKey[ProviderType.EthereumOASIS]);
            else Assert.IsFalse(source.ProviderUniqueStorageKey.ContainsKey(ProviderType.EthereumOASIS));
            var reopened=(IHolonBase)typeof(EthereumOASIS).GetMethod("DeserializePrivateStorage",BindingFlags.Static|BindingFlags.NonPublic)!
                .MakeGenericMethod(source.GetType()).Invoke(null,new object?[] {kind,new System.Numerics.BigInteger(HashUtility.GetNumericHash(source.Id.ToString())),ciphertext,source.Id})!;
            Assert.AreEqual(source.Id.ToString(),reopened.ProviderUniqueStorageKey[ProviderType.EthereumOASIS]);
            Assert.AreEqual("original mongo key",reopened.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
        }
        finally {Environment.SetEnvironmentVariable(keyName,previous);CryptographicOperations.ZeroMemory(key);}
    }
}
