using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearStorageEncryptionTests
{
    [DataTestMethod][DataRow("present")][DataRow("absent")][DataRow("wrong-context")][DataRow("invalid-shape")]
    public async Task SnapshotRetainsExactObservedCiphertextAndUsesExistingContextCodec(string mode)
    {
        const string key="holon:11111111-1111-1111-1111-111111111111",plaintext="{\"private-fixture\":\"test-only\"}";
        var service=Activator.CreateInstance(Service,new object[]{"http://127.0.0.1:1","localnet","contract.local","sender.local","unused","node",Path.Combine(AppContext.BaseDirectory,"Fixtures","storage-snapshot.mjs")})!;
        string encrypted;using(var codec=Open())encrypted=codec.Encrypt(Context(service,mode=="wrong-context"?"holon:other":key),plaintext);
        var variable="OASIS_NEAR_SNAPSHOT_FIXTURE_RESPONSE";var previous=Environment.GetEnvironmentVariable(variable);
        try{
            Environment.SetEnvironmentVariable(variable,mode=="absent"?"null":mode=="invalid-shape"?"{}":System.Text.Json.JsonSerializer.Serialize(encrypted));
            var task=(Task)Service.GetMethod("ReadSnapshotAsync")!.Invoke(service,new object[]{key})!;
            if(mode=="wrong-context"){await Assert.ThrowsExceptionAsync<AuthenticationTagMismatchException>(async()=>await task);return;}
            if(mode=="invalid-shape"){await Assert.ThrowsExceptionAsync<InvalidDataException>(async()=>await task);return;}
            await task;var snapshot=task.GetType().GetProperty("Result")!.GetValue(task)!;var type=snapshot.GetType();
            Assert.AreSame(service,type.GetProperty("Owner")!.GetValue(snapshot));Assert.AreEqual(key,type.GetProperty("Key")!.GetValue(snapshot));
            Assert.AreEqual(mode=="absent"?null:encrypted,type.GetProperty("Ciphertext")!.GetValue(snapshot));
            Assert.AreEqual(mode=="absent"?null:plaintext,type.GetProperty("Plaintext")!.GetValue(snapshot));
            Assert.IsFalse(snapshot.ToString()!.Contains(plaintext));Assert.IsFalse(snapshot.ToString()!.Contains(encrypted));
        }finally{Environment.SetEnvironmentVariable(variable,previous);}
    }
    private const string Setting="OASIS_NEAR_STORAGE_ENCRYPTION_KEY";
    private string _previous;
    private byte[] _key;
    private static readonly Type Service=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
    private static object Create(string network="localnet",string contract="contract.local")
        => Activator.CreateInstance(Service,new object[]{"http://127.0.0.1:1",network,contract,"sender.local","synthetic-unused-key","missing-node","missing-bridge"})!;
    private static EncryptedRecordCodec Open()
        => (EncryptedRecordCodec)Service.GetMethod("OpenStorageCodec",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,null)!;
    private static string Context(object service,string key)
        => (string)Service.GetMethod("StorageContext",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(service,new object[]{key})!;

    [TestInitialize]
    public void Setup(){_previous=Environment.GetEnvironmentVariable(Setting);_key=RandomNumberGenerator.GetBytes(32);Environment.SetEnvironmentVariable(Setting,Convert.ToBase64String(_key));}
    [TestCleanup]
    public void Cleanup(){Environment.SetEnvironmentVariable(Setting,_previous);CryptographicOperations.ZeroMemory(_key);}

    [TestMethod]
    public void RecordCodecEncryptsPrivateFieldsAndBindsNetworkContractAndKey()
    {
        const string value="{\"Email\":\"synthetic-private@example.invalid\",\"Verified\":true}";
        var service=Create();var context=Context(service,"avatar:owned-record");
        string envelope;
        using(var writer=Open()){envelope=writer.Encrypt(context,value);Assert.AreNotEqual(envelope,writer.Encrypt(context,value));}
        Assert.IsFalse(envelope.Contains("synthetic-private"));
        using(var reader=Open()){
            Assert.AreEqual(value,reader.Decrypt(context,envelope));
            Assert.ThrowsException<AuthenticationTagMismatchException>(()=>reader.Decrypt(Context(service,"detail:owned-record"),envelope));
            Assert.ThrowsException<AuthenticationTagMismatchException>(()=>reader.Decrypt(Context(Create("othernet"),"avatar:owned-record"),envelope));
            Assert.ThrowsException<AuthenticationTagMismatchException>(()=>reader.Decrypt(Context(Create(contract:"other.local"),"avatar:owned-record"),envelope));
            Assert.ThrowsException<CryptographicException>(()=>reader.Decrypt(context,value));
        }
        Environment.SetEnvironmentVariable(Setting,Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        using var wrong=Open();Assert.ThrowsException<AuthenticationTagMismatchException>(()=>wrong.Decrypt(context,envelope));
    }

    [DataTestMethod]
    [DataRow("GetAsync")][DataRow("ReadSnapshotAsync")][DataRow("EntriesAsync")][DataRow("PutAsync")][DataRow("DeleteAsync")]
    public async Task MissingStorageKeyFailsBeforeSdkProcessStarts(string method)
    {
        Environment.SetEnvironmentVariable(Setting,null);
        var args=method=="PutAsync"?new object[]{"avatar:owned-record","synthetic"}:new object[]{"avatar:owned-record"};
        var operation=(Task)Service.GetMethod(method)!.Invoke(Create(),args)!;
        var error=await Assert.ThrowsExceptionAsync<InvalidOperationException>(async()=>await operation);
        StringAssert.Contains(error.Message,"configured separately from the signing key");
    }
}
