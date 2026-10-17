using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

public partial class HederaPreparationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    [DataRow(false,true,false)] [DataRow(true,true,false)]
    [DataRow(false,true,true)] [DataRow(true,true,true)]
    [DataRow(false,false,false,true)]
    public async Task AdmittedEncryptedStoragePersistsOriginalBeforeControlledSubmission(bool rejectPersistence,bool routed=false,bool synchronous=false,bool cancelQuery=false)
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var names=new[]{"OASIS_HEDERA_SDK_BRIDGE","OASIS_HEDERA_STORAGE_TEST_REAL_BRIDGE","OASIS_HEDERA_STORAGE_TEST_SUBMISSIONS","OASIS_HEDERA_STORAGE_TEST_HOLD_QUERY_FILE"};
        var prior=names.ToDictionary(x=>x,Environment.GetEnvironmentVariable);
        Assert.IsFalse(string.IsNullOrWhiteSpace(prior[names[0]]),"Retained real SDK bridge required.");
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1")))directory=directory.Parent;
        Assert.IsNotNull(directory);
        var log=Path.Combine(directory!.FullName,"TestResults/SubscriptionUsageCurrent/hedera-storage-submissions-current.jsonl");
        Assert.IsFalse(File.Exists(log),"Preserve any prior failed fixture before rerun.");
        using(File.Open(log,FileMode.CreateNew,FileAccess.Write,FileShare.Read)) { }
        const string database="usage_hedera_prepared_storage_owned";
        var mongo=new MongoDB.Driver.MongoClient(connection);
        using(var databases=await mongo.ListDatabaseNamesAsync())
            CollectionAssert.DoesNotContain(await MongoDB.Driver.IAsyncCursorExtensions.ToListAsync(databases),database);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(30));var completed=false;
        try {
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions {Service="WEB6"})
                .BeginAsync("test-only",user,"hedera-storage",new string('a',64),"POST /offline-storage",
                    new UsageEndpointPolicy {MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            Environment.SetEnvironmentVariable(names[1],prior[names[0]]);
            Environment.SetEnvironmentVariable(names[2],log);
            Environment.SetEnvironmentVariable(names[0],Path.Combine(AppContext.BaseDirectory,"Fixtures/storage-prepared.mjs"));
            var provider=new HashgraphOASIS(network:"testnet",chainId:"296",storageConfiguration:new() {
                ContractId="0.0.2001",OperatorAccountId="0.0.1001",SigningKeyType="ecdsa",
                SigningKeyResolver=()=>new string('0',63)+"1",EncryptionKeyResolver=()=>new byte[32],
                GasLimit=100000,MaxFeeTinybars=100000000});
            if(cancelQuery){
                var ready=log+".ready";Assert.IsFalse(File.Exists(ready),"Preserve unexpected query fixture evidence.");
                Environment.SetEnvironmentVariable(names[3],ready);
                using var cancellation=CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
                var read=provider.ExecutePreparedStorageAsync(()=>provider.LoadHolonAsync(Guid.NewGuid(),loadChildren:false),
                    execution,"cancel-query","0.0.1001",cancellation.Token);
                while(!File.Exists(ready))await Task.Delay(25,deadline.Token);
                cancellation.Cancel();
                var cancelled=await read.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.IsTrue(cancelled.IsError);Assert.IsInstanceOfType<OperationCanceledException>(cancelled.Exception);
                Assert.AreEqual(0,File.ReadAllLines(log).Length);
                Assert.AreEqual(0,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count);
                File.Delete(ready);completed=true;return;
            }
            var manager=new ProviderManager(null,new OASISDNA {OASIS=new() {HyperDriveMode=HyperDriveModes.V2,
                StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}}) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=false};
            manager.RegisterProvider(provider);var router=new OASISHyperDrive(manager);
            var admitted=new PreparedStorageExecution(execution,"storage",new Dictionary<ProviderType,string> {{ProviderType.HashgraphOASIS,"0.0.1001"}},Array.Empty<ProviderType>());
            Task<NextGenSoftware.OASIS.Common.OASISResult<IHolon>> Save(Holon holon){
                if(!routed)return provider.SaveHolonAsync(holon,saveChildren:false);
                var request=new StorageOperationRequest {Operation="SaveHolon",Payload=holon,SaveChildren=false,
                    PreferredProvider=ProviderType.HashgraphOASIS,PreparedExecution=admitted};
                return synchronous?Task.FromResult(router.RouteRequest<IHolon>(request)):router.RouteRequestAsync<IHolon>(request);
            }
            if(rejectPersistence)await execution.BeginProviderAttemptAsync("HashgraphOASIS",null,routed?"storage.route.1.storage.1":"storage.storage.1",deadline.Token);
            async Task<NextGenSoftware.OASIS.Common.OASISResult<IHolon>> SavePair(){
                var one=await Save(new Holon {Id=Guid.NewGuid(),Name="owned secret one"});
                if(one.IsError)return one;
                return await Save(new Holon {Id=Guid.NewGuid(),Name="owned secret two"});
            }
            var result=routed?await SavePair():await provider.ExecutePreparedStorageAsync(SavePair,execution,"storage","0.0.1001",deadline.Token);
            Assert.AreEqual(rejectPersistence,result.IsError,result.Message);
            var submissions=File.ReadAllLines(log);
            Assert.AreEqual(rejectPersistence?0:2,submissions.Length);
            var durable=await store.GetAsync(execution.OperationId,deadline.Token);
            Assert.AreEqual(rejectPersistence?1:2,durable.ProviderAttempts.Count);
            if(!rejectPersistence)for(var index=0;index<2;index++){
                var attempt=durable.ProviderAttempts[index];Assert.AreEqual(routed?"storage.route."+(index+1)+".storage.1":"storage.storage."+(index+1),attempt.ClientRequestId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(attempt.NativePreparation.SignedTransactionBase64));
                Assert.IsNull(attempt.AcknowledgedAtUtc);
                using var entry=System.Text.Json.JsonDocument.Parse(submissions[index]);
                Assert.AreEqual(attempt.ProviderRequestId,entry.RootElement.GetProperty("transactionId").GetString());
            }
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.ExecutePreparedStorageAsync<int>(
                ()=>throw new InvalidOperationException("controlled operation failure"),execution,"failed","0.0.1001",deadline.Token));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.ExecutePreparedStorageAsync(
                ()=>provider.ExecutePreparedStorageAsync(()=>Task.FromResult(1),execution,"inner","0.0.1001",deadline.Token),
                execution,"outer","0.0.1001",deadline.Token));
            Assert.AreEqual(42,await provider.ExecutePreparedStorageAsync(()=>Task.FromResult(42),execution,"restored","0.0.1001",deadline.Token));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.ExecutePreparedStorageAsync(
                ()=>Task.FromResult(1),execution,"foreign","0.0.1002",deadline.Token));
            completed=true;
        } finally {
            foreach(var name in names)Environment.SetEnvironmentVariable(name,prior[name]);
            await mongo.DropDatabaseAsync(database);
            if(completed)File.Delete(log);
        }
    }
}
