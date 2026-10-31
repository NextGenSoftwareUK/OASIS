using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearCheckedPayloadTests
{
    [DataTestMethod][DataRow("preflight")][DataRow("unknown")][DataRow("failed")][DataRow("malformed")]
    public async Task CheckedWriterClassifiesActualIpcEnvelopeWithoutLosingOriginal(string mode)
    {
        const string keyName="OASIS_NEAR_STORAGE_ENCRYPTION_KEY",envelopeName="OASIS_NEAR_CHECKED_FIXTURE_ENVELOPE";
        var oldKey=Environment.GetEnvironmentVariable(keyName);var oldEnvelope=Environment.GetEnvironmentVariable(envelopeName);
        try{
            Environment.SetEnvironmentVariable(keyName,Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            var envelope=mode=="preflight"?"""{"ok":false,"error":"owned preflight refusal","checkedWritePhase":"before-submission"}""":mode=="unknown"?"""{"ok":false,"error":"owned response loss"}""":mode=="failed"?"""{"ok":true,"result":{"transactionHash":"failed-original","finalExecutionStatus":"FINAL","nativeStatus":{"Failure":{"ActionError":{"index":0}}}}}""":"""{"ok":true,"result":{"transactionHash":"known-original"}}""";
            Environment.SetEnvironmentVariable(envelopeName,envelope);
            var owner=Activator.CreateInstance(Service,new object[]{"http://127.0.0.1:1","fixture","contract.near","sender.near","unused","node",Path.Combine(AppContext.BaseDirectory,"Fixtures","storage-snapshot.mjs")})!;
            var operation=(Task)Service.GetMethod("PutRecordCheckedAsync")!.Invoke(owner,new object?[]{Snapshot(owner),"new-plaintext",null,null})!;
            Exception? error=null;try{await operation;}catch(Exception cause){error=cause;}
            Assert.IsNotNull(error,"A controlled failed/malformed envelope cannot report success.");
            if(mode=="preflight"){Assert.AreEqual("NearRecordAdmissionException",error.GetType().Name);Assert.IsFalse(NativeMutationReconciliation.IsRequired(error));StringAssert.Contains(error.Message,"owned preflight refusal");}
            else if(mode=="failed"){Assert.IsInstanceOfType<NearRecordWriteRejectedException>(error);Assert.AreEqual("failed-original",((NearRecordWriteRejectedException)error).OriginalTransactionId);Assert.IsFalse(NativeMutationReconciliation.IsRequired(error));}
            else{Assert.IsInstanceOfType<NearRecordMutationUncertainException>(error);Assert.IsTrue(NativeMutationReconciliation.IsRequired(error));Assert.AreEqual(mode=="unknown"?string.Empty:"known-original",((NearRecordMutationUncertainException)error).OriginalTransactionId);}
        }finally{Environment.SetEnvironmentVariable(keyName,oldKey);Environment.SetEnvironmentVariable(envelopeName,oldEnvelope);}
    }
    private static readonly Type Service=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
    private const string Key="holon:11111111-1111-1111-1111-111111111111";
    private static object Owner()=>Activator.CreateInstance(Service,new object[]{"http://127.0.0.1:1","fixture","contract.near","sender.near","unused","missing-node","missing-bridge"})!;
    private static object Snapshot(object owner,string key=Key)=>Activator.CreateInstance(Service.GetNestedType("StoredSnapshot",BindingFlags.NonPublic)!,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{owner,key,"exact-observed-ciphertext","old-plaintext"},null)!;
    private static Dictionary<string,object> Payload(object owner,object snapshot,string? archive=null,string? historical=null)
        =>(Dictionary<string,object>)Service.GetMethod("PrepareCheckedRecordArguments",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(owner,new object?[]{snapshot,"new-plaintext",archive,historical})!;
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void ExactOriginalCiphertextAndDistinctEncryptionContexts(bool archive)
    {
        var prior=Environment.GetEnvironmentVariable("OASIS_NEAR_STORAGE_ENCRYPTION_KEY");var bytes=RandomNumberGenerator.GetBytes(32);
        try{
            Environment.SetEnvironmentVariable("OASIS_NEAR_STORAGE_ENCRYPTION_KEY",Convert.ToBase64String(bytes));
            var owner=Owner();var archiveKey="history:"+Key+":0000000001";var args=Payload(owner,Snapshot(owner),archive?archiveKey:null,archive?"old-plaintext":null);
            Assert.AreEqual("exact-observed-ciphertext",args["expected_value"]);Assert.AreEqual(Key,args["key"]);
            using var codec=new EncryptedRecordCodec(bytes,compress:true);
            string Context(string key)=>(string)Service.GetMethod("StorageContext",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(owner,new object[]{key})!;
            Assert.AreEqual("new-plaintext",codec.Decrypt(Context(Key),(string)args["value"]));
            if(archive){Assert.AreEqual("old-plaintext",codec.Decrypt(Context(archiveKey),(string)args["archive_value"]));Assert.ThrowsException<AuthenticationTagMismatchException>(()=>codec.Decrypt(Context(Key),(string)args["archive_value"]));}
            else{Assert.IsFalse(args.ContainsKey("archive_key"));Assert.IsFalse(args.ContainsKey("archive_value"));}
        }finally{Environment.SetEnvironmentVariable("OASIS_NEAR_STORAGE_ENCRYPTION_KEY",prior);CryptographicOperations.ZeroMemory(bytes);}
    }
    [DataTestMethod][DataRow("owner")][DataRow("key")][DataRow("archive")][DataRow("pair")]
    public void ScopeAndArchiveRejectionPrecedeCodecOrNativeProcess(string mode)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_NEAR_STORAGE_ENCRYPTION_KEY");Environment.SetEnvironmentVariable("OASIS_NEAR_STORAGE_ENCRYPTION_KEY",null);
        try{
            var owner=Owner();var snapshot=Snapshot(mode=="owner"?Owner():owner,mode=="key"?"holon:invalid":Key);
            var error=Assert.ThrowsException<TargetInvocationException>(()=>Payload(owner,snapshot,mode=="archive"?"history:other:0000000001":mode=="pair"?"history:"+Key+":0000000001":null,mode=="archive"?"old":null));
            Assert.IsInstanceOfType<ArgumentException>(error.InnerException);
        }finally{Environment.SetEnvironmentVariable("OASIS_NEAR_STORAGE_ENCRYPTION_KEY",previous);}
    }
    private static string Outcome(string json)
    {
        using var response=JsonDocument.Parse(json);
        return (string)Service.GetMethod("ReadCheckedRecordOutcome",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{response.RootElement})!;
    }
    [TestMethod]public void FinalSuccessReturnsOriginalIdentity()=>Assert.AreEqual("original",Outcome("""{"transactionHash":"original","finalExecutionStatus":"FINAL","nativeStatus":{"SuccessValue":""}}"""));
    [TestMethod]public void FinalFailureIsDefiniteAndRetainsOriginalReceipt()
    {
        var error=Assert.ThrowsException<TargetInvocationException>(()=>Outcome("""{"transactionHash":"failed-original","finalExecutionStatus":"FINAL","nativeStatus":{"Failure":{"ActionError":{"index":0}}}}"""));
        var failure=(NearRecordWriteRejectedException)error.InnerException!;Assert.AreEqual("failed-original",failure.OriginalTransactionId);Assert.IsFalse(NativeMutationReconciliation.IsRequired(failure));StringAssert.Contains(failure.NativeStatusJson,"ActionError");
    }
    [DataTestMethod][DataRow("null")][DataRow("{}")][DataRow("{\"SuccessValue\":\"\",\"Failure\":{\"ActionError\":{}}}")][DataRow("{\"Failure\":{}}")][DataRow("{\"SuccessValue\":\"unexpected\"}")]
    public void InvalidAcknowledgementRetainsKnownIdentityAndBlocksReplay(string status)
    {
        var error=Assert.ThrowsException<TargetInvocationException>(()=>Outcome("{\"transactionHash\":\"known-original\",\"finalExecutionStatus\":\"FINAL\",\"nativeStatus\":"+status+"}"));
        var uncertain=(NearRecordMutationUncertainException)error.InnerException!;Assert.AreEqual("known-original",uncertain.OriginalTransactionId);Assert.IsTrue(NativeMutationReconciliation.IsRequired(uncertain));
    }
    [TestMethod]public void MissingIdentityIsNotInvented()
    {
        var error=Assert.ThrowsException<TargetInvocationException>(()=>Outcome("{}"));var uncertain=(NearRecordMutationUncertainException)error.InnerException!;Assert.AreEqual(string.Empty,uncertain.OriginalTransactionId);Assert.IsTrue(NativeMutationReconciliation.IsRequired(uncertain));
    }
}
