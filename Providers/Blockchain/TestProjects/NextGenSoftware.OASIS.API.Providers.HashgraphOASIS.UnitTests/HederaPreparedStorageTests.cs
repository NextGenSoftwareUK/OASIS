using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

public partial class HederaPreparationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task AdmittedEncryptedStoragePersistsOriginalBeforeControlledSubmission(bool rejectPersistence)
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var names=new[]{"OASIS_HEDERA_SDK_BRIDGE","OASIS_HEDERA_STORAGE_TEST_REAL_BRIDGE","OASIS_HEDERA_STORAGE_TEST_SUBMISSIONS"};
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
            if(rejectPersistence)await execution.BeginProviderAttemptAsync("HashgraphOASIS",null,"storage.storage.1",deadline.Token);
            var result=await provider.ExecutePreparedStorageAsync(async()=>{
                var one=await provider.SaveHolonAsync(new Holon {Id=Guid.NewGuid(),Name="owned secret one"},saveChildren:false);
                if(one.IsError)return one;
                return await provider.SaveHolonAsync(new Holon {Id=Guid.NewGuid(),Name="owned secret two"},saveChildren:false);
            },execution,"storage","0.0.1001",deadline.Token);
            Assert.AreEqual(rejectPersistence,result.IsError,result.Message);
            var submissions=File.ReadAllLines(log);
            Assert.AreEqual(rejectPersistence?0:2,submissions.Length);
            var durable=await store.GetAsync(execution.OperationId,deadline.Token);
            Assert.AreEqual(rejectPersistence?1:2,durable.ProviderAttempts.Count);
            if(!rejectPersistence)for(var index=0;index<2;index++){
                var attempt=durable.ProviderAttempts[index];Assert.AreEqual("storage.storage."+(index+1),attempt.ClientRequestId);
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
