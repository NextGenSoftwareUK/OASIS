using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class NearPreparedNativeTests
{
    [TestMethod][TestCategory("RetainedSignedInspection")]
    public Task RealOriginalsAreFsyncedBeforeSubmissionAndRecoveredWithoutSigningKey()=>Run(false);
    [TestMethod][TestCategory("RetainedSignedInspection")]
    public Task OrdinaryInterfacesPersistOriginalsAndRecoverWithoutSigningKey()=>Run(true);
    private static async Task Run(bool ordinary)
    {
        string Required(string name)=>Environment.GetEnvironmentVariable(name)??throw new InvalidOperationException(name+" required");
        Assert.AreEqual("http://127.0.0.1:3030",Required("NEAROASIS_RPCENDPOINT"));
        Assert.AreEqual("oasis-near-local",Required("NEAROASIS_NETWORKID"));Assert.AreEqual("test.near",Required("NEAROASIS_ACCOUNTID"));
        var root=new DirectoryInfo(Environment.CurrentDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts","run_near_provider_evidence.ps1")))root=root.Parent;
        Assert.IsNotNull(root);
        var folder=Path.Combine(root.FullName,"TestResults","NearRuntime");
        var stem=ordinary?"near-prepared-ordinary":"near-prepared-native";
        var published=Path.Combine(root.FullName,"Docs","WIP","HyperDrive-Providers","NativeEvidence",stem+".complete.json");
        var pending=Path.Combine(folder,stem+".pending.json");var journal=Path.Combine(folder,stem+".attempts.jsonl");
        var complete=Path.Combine(folder,stem+".complete.json");
        Assert.IsFalse(new[]{published,pending,journal,complete}.Any(File.Exists),"Retain/reconcile originals; never automatically finish or replay a cohort.");
        var provider=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),"oasis-near-local","oasis-near-local","test.near","test.near",Required("NEAROASIS_PRIVATEKEY"));
        var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
        using(var marker=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            marker.Write(JsonSerializer.SerializeToUtf8Bytes(new{plannedActions=3,purpose="fixed local native persistence/recovery acceptance"}));marker.Flush(true);
        }
        var records=new List<UsageProviderAttempt>();
        var args=Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"key\":\"oasis-prepared-native-read-only-probe\"}"));
        var plans=new[]{new NearNativeAction("transfer","test.near","1"),new NearNativeAction("functionCall","test.near","0","get","100000000000000",args),
            new NearNativeAction("functionCall","test.near","0","oasis_prepared_missing_method","100000000000000",Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")))};
        for(var step=0;step<plans.Length;step++){
            var store=DispatchProxy.Create<IUsageOutboxStore,FlushedOwner>();var owner=(FlushedOwner)(object)store;owner.Journal=journal;
            var execution=(UsageExecutionHandle)typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
                .Single(value=>value.GetParameters().Length==5).Invoke(new object?[]{new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),
                    ExecutionOwner="native-near-prepared-owner",Policy=new UsageEndpointPolicy()},store,new Web4UsageLedgerOptions(),null,null});
            using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(2));
            if(ordinary){
                var sdk=typeof(NEAROASIS).GetField("_sdk",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
                var plan=plans[step];
                Task<string> Invoke(){
                    var transfer=plan.Kind=="transfer";
                    var method=sdk.GetType().GetMethod(transfer?"TransferAsync":"CallAsync",BindingFlags.Instance|BindingFlags.Public)!;
                    object?[] arguments=transfer?new object?[]{plan.Recipient,plan.DepositUnits,null,null}:
                        new object?[]{plan.MethodName,JsonSerializer.Deserialize<JsonElement>(Convert.FromBase64String(plan.ArgumentsBase64)),plan.Recipient,plan.DepositUnits,null,null};
                    return (Task<string>)method.Invoke(sdk,arguments)!;
                }
                if(step<2){
                    var hash=await provider.ExecutePreparedStorageAsync(Invoke,execution,stem+"-"+step,"test.near",timeout.Token);
                    Assert.AreEqual(owner.Original.ProviderRequestId,hash);
                }else{
                    var failure=await Assert.ThrowsExceptionAsync<NearMutationRejectedException>(()=>provider.ExecutePreparedStorageAsync(Invoke,execution,stem+"-"+step,"test.near",timeout.Token));
                    Assert.AreEqual(owner.Original.ProviderRequestId,failure.OriginalTransactionId);
                }
            }else{
                var result=await provider.ExecutePreparedNativeActionAsync(plans[step],execution,stem+"-"+step,"test.near",timeout.Token);
                if(step<2){Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);Assert.IsTrue(result.IsSaved);}
                else {Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(NearMutationRejectedException));
                    Assert.AreEqual(owner.Original.ProviderRequestId,((NearMutationRejectedException)result.Exception).OriginalTransactionId);}
            }
            Assert.AreEqual(1,owner.Calls);Assert.IsNotNull(owner.Original);records.Add(owner.Original);
        }
        var reader=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),"oasis-near-local","oasis-near-local","test.near","test.near","read-only-invalid-signing-key");
        var readerActive=await reader.ActivateProviderAsync();Assert.IsFalse(readerActive.IsError,readerActive.Message);
        var outcomes=new List<JsonElement>();
        foreach(var record in records){
            var recovered=await reader.RecoverNativePreparationAsync(record);Assert.IsFalse(recovered.IsError,recovered.Exception?.ToString()??recovered.Message);
            Assert.IsTrue(recovered.Result.GetProperty("recoveredFromSignedOriginal").GetBoolean());
            Assert.AreEqual(record.NativePreparation.SignedTransactionBase64,recovered.Result.GetProperty("signedTransactionBase64").GetString());
            var observed=await reader.ReadPreparedNativeOutcomeAsync(recovered.Result);Assert.IsFalse(observed.IsError,observed.Exception?.ToString()??observed.Message);
            Assert.IsTrue(observed.IsLoaded);Assert.AreEqual(record.ProviderRequestId,observed.Result.GetProperty("transaction_outcome").GetProperty("id").GetString());
            Assert.IsTrue(observed.Result.GetProperty("status").TryGetProperty(outcomes.Count==2?"Failure":"SuccessValue",out _));outcomes.Add(observed.Result.Clone());
        }
        Assert.AreEqual(3,records.Count);Assert.AreEqual(3,records.Select(row=>row.ProviderRequestId).Distinct().Count());
        using(var file=new FileStream(complete,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{records,outcomes,physicalFsyncOwner=true,syntheticAdmission=true,hostedMongoVerified=false,
                ordinaryInterfaces=ordinary,recoveredWithoutSigningKey=true,logicalRecordWrites=0,publicFunds=false}));file.Flush(true);
        }
        var retained=File.ReadAllLines(journal).Select(line=>JsonSerializer.Deserialize<UsageProviderAttempt>(line)!).ToArray();
        CollectionAssert.AreEqual(records.Select(row=>row.ProviderRequestId).ToArray(),retained.Select(row=>row.ProviderRequestId).ToArray());
        File.Delete(journal);File.Delete(pending);
        Console.WriteLine("NEAR_PREPARED_NATIVE_VERIFIED "+JsonSerializer.Serialize(new{originals=records.Select(row=>row.ProviderRequestId).ToArray()}));
    }
    public class FlushedOwner:DispatchProxy
    {
        public string Journal=null!;public int Calls;public UsageProviderAttempt Original=null!;
        protected override object? Invoke(MethodInfo? method,object?[]? values){
            if(method?.Name!="BeginProviderAttemptAsync")throw new InvalidOperationException("Unexpected native persistence operation.");
            Original=(UsageProviderAttempt)values![2]!;
            Assert.AreEqual("NEAROASIS",Original.Provider);Assert.IsFalse(string.IsNullOrEmpty(Original.NativePreparation.SignedTransactionBase64));
            Assert.IsFalse(string.IsNullOrEmpty(Original.NativePreparation.SignDocBase64));Assert.IsNull(Original.NativePreparation.Signature);
            using(var file=new FileStream(Journal,FileMode.Append,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                file.Write(JsonSerializer.SerializeToUtf8Bytes(Original));file.WriteByte(10);file.Flush(true);
            }
            Calls++;return Task.CompletedTask;
        }
    }
}
