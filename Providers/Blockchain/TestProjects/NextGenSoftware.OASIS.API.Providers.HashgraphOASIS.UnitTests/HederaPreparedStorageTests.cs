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
    [DataRow("payment")] [DataRow("duplicate-node")] [DataRow("payer-node")] [DataRow("timeout")]
    public void InvalidPreparedStorageQueryPolicyIsRejectedBeforeResolvingSecrets(string mode)
    {
        var secrets=0;
        Assert.ThrowsException<ArgumentException>(()=>new HashgraphOASIS(network:"testnet",storageConfiguration:new(){
            ContractId="0.0.2001",OperatorAccountId="0.0.1001",SigningKeyType="ecdsa",SigningKeyResolver=()=>{secrets++;return "unneeded";},
            EncryptionKeyResolver=()=>{secrets++;return new byte[32];},GasLimit=100000,MaxFeeTinybars=100000000,
            QueryPreparation=new(){AuthorizedNodeAccountIds=mode=="duplicate-node"?new[]{"0.0.3","0.0.3"}:mode=="payer-node"?new[]{"0.0.1001"}:new[]{"0.0.3"},
                MaxPaymentTinybars=mode=="payment"?0:123,MaxTransactionFeeTinybars=100000000,PersistenceTimeoutMs=mode=="timeout"?0:10000}}));
        Assert.AreEqual(0,secrets);
    }

    [TestMethod]
    public async Task MissingPreparedStorageQueryPolicyFailsBeforeEnteringOperationOrResolvingSecrets()
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var database="usage_hedera_missing_policy_"+Guid.NewGuid().ToString("N");var mongo=new MongoDB.Driver.MongoClient(connection);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try{
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"})
                .BeginAsync("test-only",user,"missing-policy",new string('a',64),"POST /fixture",new(){MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            var secrets=0;var entered=false;
            var provider=new HashgraphOASIS(network:"testnet",storageConfiguration:new(){ContractId="0.0.2001",OperatorAccountId="0.0.1001",SigningKeyType="ecdsa",
                SigningKeyResolver=()=>{secrets++;return "unneeded";},EncryptionKeyResolver=()=>{secrets++;return new byte[32];},GasLimit=100000,MaxFeeTinybars=100000000});
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.ExecutePreparedStorageAsync(
                ()=>{entered=true;return Task.FromResult(1);},execution,"missing-policy","0.0.1001",deadline.Token));
            Assert.IsFalse(entered);Assert.AreEqual(0,secrets);Assert.AreEqual(0,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count);
            var manager=new ProviderManager(null,new OASISDNA {OASIS=new() {HyperDriveMode=HyperDriveModes.V2,
                StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}}) {IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false};
            manager.RegisterProvider(provider);var router=new OASISHyperDrive(manager);
            var admitted=new PreparedStorageExecution(execution,"missing-policy",new Dictionary<ProviderType,string> {{ProviderType.HashgraphOASIS,"0.0.1001"}},Array.Empty<ProviderType>());
            foreach(var operation in new[]{"LoadHolon","SaveHolon"})foreach(var synchronous in new[]{false,true}){
                var request=new StorageOperationRequest {Operation=operation,HolonId=Guid.NewGuid(),Payload=new Holon {Id=Guid.NewGuid()},
                    SaveChildren=false,LoadChildren=false,PreferredProvider=ProviderType.HashgraphOASIS,PreparedExecution=admitted};
                var result=synchronous?router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                Assert.IsTrue(result.IsError);Assert.AreEqual(PreparedStorageExecution.RejectedCode,result.ErrorCode,result.Message);
                Assert.IsFalse(provider.IsProviderActivated,"Admission failure must prevent activation for reads and writes.");
                Assert.AreEqual(0,secrets,"Rejected prepared routing must not resolve signing or encryption secrets.");
                Assert.AreEqual(0,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count);
            }
        }finally{await mongo.DropDatabaseAsync(database);}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    [DataRow(false,true,false)] [DataRow(true,true,false)]
    [DataRow(false,true,true)] [DataRow(true,true,true)]
    [DataRow(false,false,false,true)]
    [DataRow(false,true,false,false,true)] [DataRow(false,true,true,false,true)]
    public async Task AdmittedEncryptedStoragePersistsOriginalBeforeControlledSubmission(bool rejectPersistence,bool routed=false,bool synchronous=false,bool cancelQuery=false,bool attemptBudget=false)
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
                    new UsageEndpointPolicy {MeterCategory="api.request",ReservedUnits=1,MaximumProviderAttempts=attemptBudget?1:512},deadline.Token);
            var priceTime=DateTime.UtcNow.AddSeconds(-1);
            var price=new NativeFeePriceQuote(Guid.Parse(execution.OperationId),"HashgraphOASIS","testnet","tinybar","hedera-hashgraph",8,1m,
                "synthetic-offline-price","fixture-v1","fixture-v1",priceTime,priceTime,priceTime,priceTime.AddMinutes(5));
            if(!rejectPersistence)execution=execution.WithNativePrices(new[]{price});
            Environment.SetEnvironmentVariable(names[1],prior[names[0]]);
            Environment.SetEnvironmentVariable(names[2],log);
            Environment.SetEnvironmentVariable(names[0],Path.Combine(AppContext.BaseDirectory,"Fixtures/storage-prepared.mjs"));
            var policyNodes=new[]{"0.0.3"};
            var secretResolutions=0;
            var provider=new HashgraphOASIS(network:"testnet",chainId:"296",storageConfiguration:new() {
                ContractId="0.0.2001",OperatorAccountId="0.0.1001",SigningKeyType="ecdsa",
                SigningKeyResolver=()=>{secretResolutions++;return new string('0',63)+"1";},EncryptionKeyResolver=()=>new byte[32],
                GasLimit=100000,MaxFeeTinybars=100000000,QueryPreparation=new(){AuthorizedNodeAccountIds=policyNodes,MaxPaymentTinybars=123,MaxTransactionFeeTinybars=100000000}});
            policyNodes[0]="0.0.4"; // Configured policy must retain its independent admitted node snapshot.
            var entered=false;
            foreach(var length in new[]{110,128})
                await Assert.ThrowsExceptionAsync<ArgumentException>(()=>provider.ExecutePreparedStorageAsync(
                    ()=>{entered=true;return Task.FromResult(true);},execution,new string('x',length),"0.0.1001",deadline.Token));
            Assert.IsFalse(entered,"Invalid correlation must fail before entering storage or preparing a signature.");
            Assert.AreEqual(0,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count);
            Assert.AreEqual(0,File.ReadAllLines(log).Length);
            Assert.IsTrue(await provider.ExecutePreparedStorageAsync(()=>Task.FromResult(true),execution,new string('x',109),
                "0.0.1001",deadline.Token));
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
            HolonManager holonManager=null;
            Task<NextGenSoftware.OASIS.Common.OASISResult<IHolon>> Save(Holon holon){
                if(!routed)return provider.SaveHolonAsync(holon,saveChildren:false);
                return synchronous?Task.FromResult(holonManager.SaveHolon(holon,Guid.Parse(user),saveChildren:false))
                    :holonManager.SaveHolonAsync(holon,Guid.Parse(user),saveChildren:false);
            }
            if(rejectPersistence){
                var parent=routed?"storage.route.1.storage.1":"storage.storage.1";
                var query="query."+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(parent))).ToLowerInvariant()+".1";
                await execution.BeginProviderAttemptAsync("HashgraphOASIS",null,query,deadline.Token);
            }
            async Task<NextGenSoftware.OASIS.Common.OASISResult<IHolon>> SavePair(){
                var one=await Save(new Holon {Id=Guid.NewGuid(),Name="owned secret one"});
                if(one.IsError)return one;
                return await Save(new Holon {Id=Guid.NewGuid(),Name="owned secret two"});
            }
            var result=routed?await admitted.RunAsync(async()=>{
                var before=(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count;
                var secretsBefore=secretResolutions;
                // Invoke the existing injected-runtime constructor, avoiding process-global test state.
                holonManager=(HolonManager)Activator.CreateInstance(typeof(HolonManager),
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,
                    new object[]{provider,manager.OASISDNA,manager},null)!;
                Assert.IsFalse(provider.IsProviderActivated,"Constructor must leave activation to admitted routing.");
                Assert.AreEqual(secretsBefore,secretResolutions,"Constructor must not resolve a paid-query signing secret.");
                Assert.AreEqual(before,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count);
                Assert.AreEqual(0,File.ReadAllLines(log).Length);
                return await SavePair();
            }):await provider.ExecutePreparedStorageAsync(SavePair,execution,"storage","0.0.1001",deadline.Token);
            if(attemptBudget){
                Assert.IsTrue(result.IsError,result.Message);
                bool HasBudgetCause(Exception? error)=>error is UsageProtocolException protocol&&protocol.Code=="PROVIDER_ATTEMPT_BUDGET_EXCEEDED"
                    ||error is AggregateException aggregate&&aggregate.InnerExceptions.Any(HasBudgetCause)
                    ||error?.InnerException!=null&&HasBudgetCause(error.InnerException);
                Assert.IsTrue(HasBudgetCause(result.Exception),"Routed activation must preserve its original durable budget rejection.");
                Assert.IsFalse(provider.IsProviderActivated);
                Assert.AreEqual(0,File.ReadAllLines(log).Length,"No storage mutation may be submitted after query admission exhausted the operation budget.");
                var retained=await new MongoUsageOutboxStore(connection,database).GetAsync(execution.OperationId,deadline.Token);
                Assert.AreEqual(1,retained.Policy.MaximumProviderAttempts);Assert.AreEqual(1,retained.ProviderAttempts.Count);
                var query=retained.ProviderAttempts.Single();
                Assert.AreEqual("hedera-query-payment-v1",query.NativePreparation.SigningAuthority.Scheme);
                Assert.AreEqual(price,query.NativePreparation.PriceQuotes.Single());
                Assert.IsFalse(string.IsNullOrWhiteSpace(query.NativePreparation.SignedTransactionBase64));
                Assert.IsNull(query.AcknowledgedAtUtc);Assert.IsNull(retained.Settlement);
                completed=true;return;
            }
            Assert.AreEqual(rejectPersistence,result.IsError,result.Message);
            var submissions=File.ReadAllLines(log);
            Assert.AreEqual(rejectPersistence?0:2,submissions.Length);
            var durable=await store.GetAsync(execution.OperationId,deadline.Token);
            // First routed activation performs a paid probe and private index listing before the save's read/write.
            Assert.AreEqual(rejectPersistence?1:routed?6:4,durable.ProviderAttempts.Count);
            var writes=durable.ProviderAttempts.Where(attempt=>attempt.NativePreparation?.SigningAuthority?.Scheme=="hedera-payer-public-key-v1").ToArray();
            if(!rejectPersistence)for(var index=0;index<2;index++){
                var attempt=writes[index];Assert.AreEqual(routed?"storage.route."+(index+1)+".storage."+(index==0?4:2):"storage.storage."+(2*(index+1)),attempt.ClientRequestId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(attempt.NativePreparation.SignedTransactionBase64));
                Assert.AreEqual(price,attempt.NativePreparation.PriceQuotes.Single());
                Assert.IsNull(attempt.AcknowledgedAtUtc);
                using var entry=System.Text.Json.JsonDocument.Parse(submissions[index]);
                Assert.AreEqual(attempt.ProviderRequestId,entry.RootElement.GetProperty("transactionId").GetString());
            }
            if(!rejectPersistence){
                var queries=durable.ProviderAttempts.Where(attempt=>attempt.NativePreparation.SigningAuthority.Scheme=="hedera-query-payment-v1").ToArray();
                Assert.AreEqual(routed?4:2,queries.Length);
                foreach(var query in queries){Assert.AreEqual(price,query.NativePreparation.PriceQuotes.Single());Assert.IsNull(query.AcknowledgedAtUtc);}
                if(routed){
                    var request=new StorageOperationRequest {Operation="LoadHolon",HolonId=Guid.NewGuid(),LoadChildren=false,
                        PreferredProvider=ProviderType.HashgraphOASIS};
                    var read=await admitted.RunAsync(async()=>synchronous?router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request));
                    Assert.AreSame(admitted,request.PreparedExecution);
                    Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,read.ErrorCode,read.Message);
                    Assert.IsNull(read.Result); // A missing logical record still incurs the admitted paid read.
                    var afterRead=await store.GetAsync(execution.OperationId,deadline.Token);
                    Assert.AreEqual(durable.ProviderAttempts.Count+1,afterRead.ProviderAttempts.Count,"Standalone routed read must retain its paid query.");
                    Assert.AreEqual(2,File.ReadAllLines(log).Length,"Standalone read must not submit a mutation.");
                    var retainedRead=afterRead.ProviderAttempts.Last();
                    Assert.AreEqual("hedera-query-payment-v1",retainedRead.NativePreparation.SigningAuthority.Scheme);
                    Assert.AreEqual(price,retainedRead.NativePreparation.PriceQuotes.Single());
                }
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
