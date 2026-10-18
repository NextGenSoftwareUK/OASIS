using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass,DoNotParallelize,TestCategory("FundedTestnet")]
public class HederaPreparedNativeReadTests
{
    [TestMethod,TestCategory("ReadOnlyReconciliation")]
    public async Task ArchiveExactReviewedPreExecutionReadFailureWithoutPayment()
    {
        await InspectRetainedFailedReadLedgerWithoutSigningOrMutation();
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/inspect-reusable-hedera-store.mjs")))root=root.Parent;
        Assert.IsNotNull(root);var work=Path.Combine(root!.FullName,"TestResults/HederaRuntime");
        var journal=Path.Combine(work,"prepared-read-pending.json");var inspection=Path.Combine(work,"prepared-read-ledger-inspection.json");
        string Hash(string file)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        Assert.AreEqual("2E14523D7689895A3350989A001E2F02859ACD427749F66E6A9001589CC6C70B",Hash(inspection));
        // Reviewed against failed-run parent76a6ead71: selected-request admission,
        // query construction selection and transport tail are unchanged byte-for-byte after LF normalization.
        var module=File.ReadAllText(Path.Combine(root.FullName,"Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/paid-query-preparation.mjs")).Replace("\r\n","\n");
        var tail=module[module.IndexOf("    async _executeRequestWithGrpcDeadline",StringComparison.Ordinal)..];
        var admissionTailHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tail)));
        Assert.AreEqual("911412B836BE8BE78CAF3DCF03FA57C105E47E67C360396D18ACD98BFE5AA9A0",admissionTailHash);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var http=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false});
        async Task<JsonDocument> Mirror(string route){
            using var response=await http.GetAsync("https://testnet.mirrornode.hedera.com/api/v1/"+route,HttpCompletionOption.ResponseHeadersRead,deadline.Token);
            response.EnsureSuccessStatusCode();using var input=await response.Content.ReadAsStreamAsync(deadline.Token);using var bytes=new MemoryStream();
            var buffer=new byte[4096];int read;while((read=await input.ReadAsync(buffer,deadline.Token))>0){if(bytes.Length+read>262144)throw new InvalidDataException("Bounded mirror response required.");bytes.Write(buffer,0,read);}
            return JsonDocument.Parse(bytes.ToArray());
        }
        using var account=await Mirror("transactions?account.id=0.0.10993779&timestamp=gte%3A1792281990&timestamp=lte%3A1792282020&limit=100&order=asc");
        Assert.AreEqual(0,account.RootElement.GetProperty("transactions").GetArrayLength());Assert.AreEqual(JsonValueKind.Null,account.RootElement.GetProperty("links").GetProperty("next").ValueKind);
        using var latest=await Mirror("transactions?limit=1&order=desc");
        var indexed=latest.RootElement.GetProperty("transactions")[0].GetProperty("consensus_timestamp").GetString()!;
        Assert.IsTrue(decimal.Parse(indexed,CultureInfo.InvariantCulture)>1792282020m);
        using var original=JsonDocument.Parse(File.ReadAllBytes(journal));using var live=JsonDocument.Parse(File.ReadAllBytes(inspection));
        var database="usage_hedera_native_read";var mongo=new MongoClient("mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000");
        var collection=mongo.GetDatabase(database).GetCollection<MongoDB.Bson.BsonDocument>("subscription_usage_outbox_v1");
        Assert.AreEqual(1L,await collection.CountDocumentsAsync(MongoDB.Bson.BsonDocument.Parse("{}"),cancellationToken:deadline.Token));
        var archive=Path.Combine(work,"reviewed-pre-execution-read-failure.json");Assert.IsFalse(File.Exists(archive));
        using(var file=new FileStream(archive,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            await JsonSerializer.SerializeAsync(file,new{originalJournal=original.RootElement,liveInspection=live.RootElement,admissionTailHash,
                reviewedFailedRunParent="76a6ead71",accountWindow=account.RootElement,latestIndexedConsensusTimestamp=indexed,
                preExecutionReviewed=true,basis="Exact zero-attempt durable record plus unchanged mandatory persistence-before-paid-transport invariant; mirror absence is corroboration only.",
                paidSdkQueries=0,signingKeyLoaded=false,settlementAuthorized=false,generalZeroAttemptRetryPolicy=false},cancellationToken:deadline.Token);file.Flush(true);
        }
        using(var proof=JsonDocument.Parse(File.ReadAllBytes(archive)))Assert.IsTrue(proof.RootElement.GetProperty("preExecutionReviewed").GetBoolean());
        Assert.AreEqual("0BC4E16A8A7256708615F7C113F952664B475FC05294CCBDF8F87581ED58628F",Hash(journal));
        Console.WriteLine("Exact pre-execution failure archived; no paid request or settlement. Archive SHA256="+Hash(archive));
        await mongo.DropDatabaseAsync(database,deadline.Token);File.Delete(journal);
    }
    [TestMethod,TestCategory("ReadOnlyReconciliation")]
    public async Task InspectRetainedFailedReadLedgerWithoutSigningOrMutation()
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/inspect-reusable-hedera-store.mjs")))root=root.Parent;
        Assert.IsNotNull(root);
        var journal=Path.Combine(root!.FullName,"TestResults/HederaRuntime/prepared-read-pending.json");
        var failed=Path.Combine(root.FullName,"TestResults/SubscriptionUsageCurrent/hedera-native-prepared-read-current.trx");
        string Hash(string file)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        const string journalHash="0BC4E16A8A7256708615F7C113F952664B475FC05294CCBDF8F87581ED58628F";
        Assert.AreEqual(journalHash,Hash(journal));Assert.AreEqual("2B49D7F5196A1153E9A1181503D4E755B58994010BEFE5987B45C5A7225FF81A",Hash(failed));
        using var intent=JsonDocument.Parse(File.ReadAllBytes(journal));
        var operationId=intent.RootElement.GetProperty("operationId").GetString()!;
        Assert.AreEqual("3cabee73-6a12-f4e6-8dfa-8c443136ff47",operationId);
        Assert.AreEqual("usage_hedera_native_read",intent.RootElement.GetProperty("database").GetString());
        var report=System.Xml.Linq.XDocument.Load(failed);
        var text=report.Descendants().Single(row=>row.Name.LocalName=="UnitTestResult").Descendants().Single(row=>row.Name.LocalName=="StdOut").Value;
        using var retained=JsonDocument.Parse(text.Split('\n').Single(row=>row.StartsWith("{\"retainedOriginal\":",StringComparison.Ordinal)));
        var historical=retained.RootElement.GetProperty("retainedOriginal");
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var store=new MongoUsageOutboxStore(connection,"usage_hedera_native_read");
        var live=await store.GetAsync(operationId,deadline.Token);Assert.IsNotNull(live);
        var snapshot=JsonSerializer.SerializeToElement(live);
        foreach(var field in new[]{"Id","UserId","Service","Fingerprint","Endpoint","State","ExecutionOwner"})
            Assert.AreEqual(historical.GetProperty(field).GetString(),snapshot.GetProperty(field).GetString(),field);
        Assert.AreEqual(operationId,live.Id);Assert.AreEqual("executing",live.State);
        foreach(var field in new[]{"ProviderAttempts","Receipts","AdministrativeActions"}){
            Assert.AreEqual(0,historical.GetProperty(field).GetArrayLength());Assert.AreEqual(0,snapshot.GetProperty(field).GetArrayLength());
        }
        foreach(var field in new[]{"NativeFeeExposureUsd","NativePreparationBytes","EvidenceRevision"})
            Assert.AreEqual(historical.GetProperty(field).GetDecimal(),snapshot.GetProperty(field).GetDecimal(),field);
        Assert.AreEqual(0m,live.NativeFeeExposureUsd);Assert.AreEqual(0,live.NativePreparationBytes);
        foreach(var field in new[]{"Settlement","ObservedOutcome","SettledAtUtc"})Assert.AreEqual(JsonValueKind.Null,snapshot.GetProperty(field).ValueKind,field);
        Assert.AreEqual(journalHash,Hash(journal));
        var evidence=new{originalJournalSha256=journalHash,failedReportSha256=Hash(failed),liveRecord=live,
            readOnly=true,signingKeyLoaded=false,paidSdkQueries=0,journalRemoved=false,databaseRemoved=false,mutationAuthorized=false};
        var output=Path.Combine(root.FullName,"TestResults/HederaRuntime/prepared-read-ledger-inspection.json");
        using(var file=new FileStream(output,FileMode.Create,FileAccess.Write,FileShare.None)){await JsonSerializer.SerializeAsync(file,evidence,cancellationToken:deadline.Token);file.Flush(true);}
        Console.WriteLine("Retained failed Hedera read ledger matches original snapshot;0attempts/receipts/exposure; no signing, mutation or journal deletion. Evidence SHA256="+Hash(output));
    }
    private sealed class Authority(string user):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
            var body=await request.Content!.ReadAsStringAsync(token);object response;
            if(request.RequestUri!.AbsolutePath.EndsWith("authorize")){
                var input=JsonSerializer.Deserialize<UsageAuthorizationRequest>(body)!;
                response=new SubscriptionAuthorizationResult{Allowed=true,StatusCode=200,Status="authorized",UserId=user,OperationId=input.OperationId,ExpiresAtUtc=DateTime.UtcNow.AddMinutes(5)};
            }else if(request.RequestUri.AbsolutePath.EndsWith("start")){
                var input=JsonSerializer.Deserialize<UsageStartRequest>(body)!;response=new UsageStartResult{Started=true,Status="executing",OperationId=input.OperationId};
            }else throw new InvalidOperationException("This native-read fixture never delivers hosted settlement.");
            return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(response),Encoding.UTF8,"application/json")};
        }
    }
    [TestMethod]
    public async Task RetainedTestnetReadPreparesEveryPaymentBeforeTransportAndRecoversActualFees()
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("0.0.10993779",Environment.GetEnvironmentVariable("OASIS_HEDERA_PREPARED_READ_ACCOUNT_ID"));
        var key=Environment.GetEnvironmentVariable("OASIS_HEDERA_PREPARED_READ_PRIVATE_KEY");Assert.IsFalse(string.IsNullOrWhiteSpace(key));
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/inspect-reusable-hedera-store.mjs")))root=root.Parent;
        Assert.IsNotNull(root);var statePath=Path.Combine(root!.FullName,"TestResults/HederaRuntime/deployment-store.json");
        var stateHash=SHA256.HashData(File.ReadAllBytes(statePath));
        using var state=JsonDocument.Parse(File.ReadAllText(statePath));Assert.AreEqual("0.0.11017885",state.RootElement.GetProperty("contractId").GetString());Assert.IsTrue(state.RootElement.GetProperty("verified").GetBoolean());Assert.IsFalse(state.RootElement.TryGetProperty("pending",out _));
        const string database="usage_hedera_native_read";var mongo=new MongoClient(connection);
        using(var names=await mongo.ListDatabaseNamesAsync())Assert.IsFalse((await names.ToListAsync()).Contains(database));
        var journal=Path.Combine(root.FullName,"TestResults/HederaRuntime/prepared-read-pending.json");Assert.IsFalse(File.Exists(journal));
        using var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(3));var token=deadline.Token;var complete=false;
        var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(token);string? operationId=null;
        var provider=new HashgraphOASIS("https://testnet.mirrornode.hedera.com","testnet","296",storageConfiguration:new(){
            ContractId="0.0.11017885",OperatorAccountId="0.0.10993779",SigningKeyType="der",SigningKeyResolver=()=>key!,
            EncryptionKeyResolver=()=>SHA256.HashData(Encoding.UTF8.GetBytes("oasis-hedera-testnet-storage-fixture-v1")),GasLimit=100000,MaxFeeTinybars=1000000,
            QueryPreparation=new(){AuthorizedNodeAccountIds=Enumerable.Range(3,7).Select(number=>"0.0."+number).ToArray(),MaxPaymentTinybars=1000000,MaxTransactionFeeTinybars=1000000,MaximumRequestsPerInvocation=6}});
        try{
            var user=Guid.NewGuid().ToString("D");using var transport=new HttpClient(new Authority(user));
            var execution=await new Web4UsageExecution(new Web4SubscriptionAuthorizationClient(transport,"https://controlled-authority.invalid","WEB6","test-only"),store,new(){Service="WEB6"})
                .BeginAsync("test-only",user,"retained-native-read",new string('a',64),"GET /native-read-fixture",new(){MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=.1m,MaximumNativeFeeCostUsd=.1m,MaximumProviderAttempts=6},token);
            operationId=execution.OperationId;var time=DateTime.UtcNow.AddSeconds(-1);
            execution=execution.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(operationId),"HashgraphOASIS","testnet","tinybar","hedera-hashgraph",8,1m,
                "synthetic-testnet-price","fixture-v1","fixture-v1",time,time,time,time.AddMinutes(5))});
            var holonId=Guid.NewGuid();await JsonSerializer.SerializeAsync(file,new{database,operationId,holonId,contractId="0.0.11017885",payer="0.0.10993779",maximumTestHbar=.1m,
                recovery="Read original query payments only; never retry an uncertain paid read or remove this fence without reconciliation."},cancellationToken:token);file.Flush(true);
            var read=await provider.ExecutePreparedStorageAsync(()=>provider.LoadHolonAsync(holonId,loadChildren:false),execution,"native-read","0.0.10993779",token);
            Assert.IsNull(read.Result);Assert.IsTrue(!read.IsError||read.ErrorCode==NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,read.Message);
            var operation=await store.GetAsync(operationId,token);Assert.IsTrue(operation.ProviderAttempts.Count>0&&operation.ProviderAttempts.Count<=6);
            decimal maximum=0;var originals=new List<object>();
            foreach(var attempt in operation.ProviderAttempts){
                Assert.AreEqual("hedera-query-payment-v1",attempt.NativePreparation.SigningAuthority.Scheme);Assert.IsNull(attempt.AcknowledgedAtUtc);
                maximum+=NativeFeeMaximumAccounting.ValueUsd(attempt.NativePreparation.FeeMaximums,attempt.NativePreparation.PriceQuotes);
                NextGenSoftware.OASIS.Common.OASISResult<JsonElement>? fee=null;var until=DateTime.UtcNow.AddSeconds(40);
                while(DateTime.UtcNow<until){fee=await provider.GetPreparedNativeFeeReceiptAsync(attempt,token);if(!fee.IsError)break;await Task.Delay(2000,token);}
                Assert.IsNotNull(fee);Assert.IsFalse(fee.IsError,fee.Message);Assert.IsTrue(fee.Result.GetProperty("success").GetBoolean());
                Assert.IsTrue(System.Numerics.BigInteger.Parse(fee.Result.GetProperty("netUnits").GetString()!,CultureInfo.InvariantCulture)>0);
                originals.Add(new{attempt,actualFee=fee.Result});
            }
            Assert.IsTrue(maximum>0&&maximum<=.1m);Assert.AreEqual(maximum,operation.NativeFeeExposureUsd);
            var owner=new PreparedNativeUsageRecoveryResolver(store,new[]{(NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader)provider});
            Assert.IsNull(await owner.ResolveAsync(operation,token));var measured=await store.GetAsync(operationId,token);Assert.AreEqual(operation.ProviderAttempts.Count,measured.Receipts.Count);Assert.IsNull(measured.Settlement);
            Console.WriteLine(JsonSerializer.Serialize(new{originals,measured.Receipts,maximumTestHbar=.1m,measured.NativeFeeExposureUsd,hostedSettlementVerified=false,storageMutations=0}));
            CollectionAssert.AreEqual(stateHash,SHA256.HashData(File.ReadAllBytes(statePath)));complete=true;
        }catch(Exception error){
            Console.WriteLine("Native read failed; original journal retained: "+error.Message);
            if(operationId!=null)try{using var evidenceDeadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));Console.WriteLine(JsonSerializer.Serialize(new{retainedOriginal=await store.GetAsync(operationId,evidenceDeadline.Token)}));}
                catch(Exception lookupFailure){Console.WriteLine("Retained evidence lookup failed; inspect original journal: "+lookupFailure.GetType().Name);}
            throw;
        }finally{
            await provider.DeActivateProviderAsync();if(complete){await mongo.DropDatabaseAsync(database,token);file.Dispose();File.Delete(journal);}
        }
    }
}
