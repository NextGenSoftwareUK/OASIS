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
        var nodeProfile=Hash(journal)=="D7984BB5DF86BDBD4D829F3B5A286FD2DEA1B33CEE7E9073D786082DC64A0385";
        var routedProfile=Hash(journal)=="2E4BDBB2E346E93D043B9A9FACCE5123F51A03540B827C58038E21AFB4081080";
        if(!nodeProfile&&!routedProfile)Assert.AreEqual("2E14523D7689895A3350989A001E2F02859ACD427749F66E6A9001589CC6C70B",Hash(inspection));
        // Reviewed against failed-run parent76a6ead71: selected-request admission,
        // query construction selection and transport tail are unchanged byte-for-byte after LF normalization.
        var module=File.ReadAllText(Path.Combine(root.FullName,"Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/paid-query-preparation.mjs")).Replace("\r\n","\n");
        var tail=routedProfile?File.ReadAllText(Path.Combine(root.FullName,"OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.Register.cs")).Replace("\r\n","\n")
            :nodeProfile?module[module.IndexOf("function preparedQuery",StringComparison.Ordinal)..module.IndexOf("    async _makePaymentTransaction",StringComparison.Ordinal)]
            +module[module.IndexOf("    async _executeRequestWithGrpcDeadline",StringComparison.Ordinal)..module.IndexOf("export const PreparedAccountInfoQuery",StringComparison.Ordinal)]
            :module[module.IndexOf("    async _executeRequestWithGrpcDeadline",StringComparison.Ordinal)..];
        var admissionTailHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tail)));
        Assert.AreEqual(routedProfile?"C47365C99DDE8D185C7B02055760E48EE2592DDF8E69F547BA1A43FBE120F224":nodeProfile?"A032CCD5BACC1CA1E04D63CDD2F472EB2EA38CD65A51B303346ED08A9357D217":"911412B836BE8BE78CAF3DCF03FA57C105E47E67C360396D18ACD98BFE5AA9A0",admissionTailHash);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var http=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false});
        async Task<JsonDocument> Mirror(string route){
            using var response=await http.GetAsync("https://testnet.mirrornode.hedera.com/api/v1/"+route,HttpCompletionOption.ResponseHeadersRead,deadline.Token);
            response.EnsureSuccessStatusCode();using var input=await response.Content.ReadAsStreamAsync(deadline.Token);using var bytes=new MemoryStream();
            var buffer=new byte[4096];int read;while((read=await input.ReadAsync(buffer,deadline.Token))>0){if(bytes.Length+read>262144)throw new InvalidDataException("Bounded mirror response required.");bytes.Write(buffer,0,read);}
            return JsonDocument.Parse(bytes.ToArray());
        }
        var start=routedProfile?DateTimeOffset.Parse("2026-10-18T01:27:15Z").ToUnixTimeSeconds():nodeProfile?1792285960:1792281990;
        var end=routedProfile?DateTimeOffset.Parse("2026-10-18T01:27:45Z").ToUnixTimeSeconds():nodeProfile?1792285990:1792282020;
        using var account=await Mirror($"transactions?account.id=0.0.10993779&timestamp=gte%3A{start}&timestamp=lte%3A{end}&limit=100&order=asc");
        Assert.AreEqual(0,account.RootElement.GetProperty("transactions").GetArrayLength());Assert.AreEqual(JsonValueKind.Null,account.RootElement.GetProperty("links").GetProperty("next").ValueKind);
        using var latest=await Mirror("transactions?limit=1&order=desc");
        var indexed=latest.RootElement.GetProperty("transactions")[0].GetProperty("consensus_timestamp").GetString()!;
        Assert.IsTrue(decimal.Parse(indexed,CultureInfo.InvariantCulture)>end);
        using var original=JsonDocument.Parse(File.ReadAllBytes(journal));using var live=JsonDocument.Parse(File.ReadAllBytes(inspection));
        var database="usage_hedera_native_read";var mongo=new MongoClient("mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000");
        var collection=mongo.GetDatabase(database).GetCollection<MongoDB.Bson.BsonDocument>("subscription_usage_outbox_v1");
        Assert.AreEqual(1L,await collection.CountDocumentsAsync(MongoDB.Bson.BsonDocument.Parse("{}"),cancellationToken:deadline.Token));
        var archive=Path.Combine(work,routedProfile?"reviewed-routed-configuration-pre-execution-failure.json":nodeProfile?"reviewed-node-inventory-pre-execution-failure.json":"reviewed-pre-execution-read-failure.json");Assert.IsFalse(File.Exists(archive));
        using(var file=new FileStream(archive,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            await JsonSerializer.SerializeAsync(file,new{originalJournal=original.RootElement,liveInspection=live.RootElement,admissionTailHash,
                reviewedFailedRunParent=routedProfile?"7af11eac4":nodeProfile?"cd2709376":"76a6ead71",accountWindow=account.RootElement,latestIndexedConsensusTimestamp=indexed,
                preExecutionReviewed=true,basis=routedProfile?"Exact zero-attempt durable record and failed stack at configuration access line671 before provider activation line674; Core4e61bcd source hash retained. Mirror absence is corroboration only.":"Exact zero-attempt durable record plus unchanged mandatory persistence-before-paid-transport invariant; mirror absence is corroboration only.",
                paidSdkQueries=0,signingKeyLoaded=false,settlementAuthorized=false,generalZeroAttemptRetryPolicy=false},cancellationToken:deadline.Token);file.Flush(true);
        }
        using(var proof=JsonDocument.Parse(File.ReadAllBytes(archive)))Assert.IsTrue(proof.RootElement.GetProperty("preExecutionReviewed").GetBoolean());
        Assert.AreEqual(routedProfile?"2E4BDBB2E346E93D043B9A9FACCE5123F51A03540B827C58038E21AFB4081080":nodeProfile?"D7984BB5DF86BDBD4D829F3B5A286FD2DEA1B33CEE7E9073D786082DC64A0385":"0BC4E16A8A7256708615F7C113F952664B475FC05294CCBDF8F87581ED58628F",Hash(journal));
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
        if(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(journal)))=="D1ECE5EC7741321656DEFA52C8BD564E020DCED7342B31F4FF521F8CD1E34A81"){
            await InspectExactRoutedActivationPayments(root.FullName,journal);return;
        }
        string Hash(string file)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        var routedProfile=Hash(journal)=="2E4BDBB2E346E93D043B9A9FACCE5123F51A03540B827C58038E21AFB4081080";
        var failed=Path.Combine(root.FullName,routedProfile?"TestResults/SubscriptionUsageCurrent/hedera-native-routed-read-current.trx":"TestResults/SubscriptionUsageCurrent/hedera-native-prepared-read-current.trx");
        var nodeProfile=Hash(journal)=="D7984BB5DF86BDBD4D829F3B5A286FD2DEA1B33CEE7E9073D786082DC64A0385";
        var journalHash=routedProfile?"2E4BDBB2E346E93D043B9A9FACCE5123F51A03540B827C58038E21AFB4081080":nodeProfile?"D7984BB5DF86BDBD4D829F3B5A286FD2DEA1B33CEE7E9073D786082DC64A0385":"0BC4E16A8A7256708615F7C113F952664B475FC05294CCBDF8F87581ED58628F";
        Assert.AreEqual(journalHash,Hash(journal));Assert.AreEqual(routedProfile?"9345F5C3292316F6899A1A3771CCC73370AF8B0A641F2D31402000B9F88E31FB":nodeProfile?"1517622E1E624CC4DD1D1E224DE9FF4E7139CAF409E7F41892C406B0A48D34C0":"2B49D7F5196A1153E9A1181503D4E755B58994010BEFE5987B45C5A7225FF81A",Hash(failed));
        using var intent=JsonDocument.Parse(File.ReadAllBytes(journal));
        var operationId=intent.RootElement.GetProperty("operationId").GetString()!;
        Assert.AreEqual(routedProfile?"9b9ea6ea-0981-79c9-3104-4f9a76641750":nodeProfile?"a6a4a4f8-5fe0-f6da-7dc0-b5e8f21f828e":"3cabee73-6a12-f4e6-8dfa-8c443136ff47",operationId);
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
    private static async Task InspectExactRoutedActivationPayments(string root,string journal)
    {
        string Hash(string file)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        const string journalHash="D1ECE5EC7741321656DEFA52C8BD564E020DCED7342B31F4FF521F8CD1E34A81";
        Assert.AreEqual(journalHash,Hash(journal));
        Assert.AreEqual("A8399DD51E00CC60D0A8F22D8E83A0467BA16943084B882E6A94431CA7C22FFC",Hash(Path.Combine(root,"TestResults/SubscriptionUsageCurrent/hedera-native-routed-read-current.trx")));
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var store=new MongoUsageOutboxStore(connection,"usage_hedera_native_read");using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(40));
        var original=await store.GetAsync("222c0b6b-625a-8198-564d-08b7f55ff929",deadline.Token);
        Assert.AreEqual(4,original.ProviderAttempts.Count);Assert.AreEqual(.28294688m,original.NativeFeeExposureUsd);Assert.IsNull(original.Settlement);Assert.AreEqual(0,original.Receipts.Count);
        var before=JsonSerializer.SerializeToElement(original);
        var reader=new HashgraphOASIS("https://testnet.mirrornode.hedera.com","testnet","296");var fees=new List<object>();long debit=0;
        try{
            foreach(var attempt in original.ProviderAttempts){
                var fee=await reader.GetPreparedNativeFeeReceiptAsync(attempt,deadline.Token);Assert.IsFalse(fee.IsError,fee.Message);
                Assert.IsTrue(fee.Result.GetProperty("success").GetBoolean());Assert.AreEqual("0.0.10993779",fee.Result.GetProperty("payer").GetString());
                debit=checked(debit+long.Parse(fee.Result.GetProperty("netUnits").GetString()!,CultureInfo.InvariantCulture));
                fees.Add(new{attempt.ProviderRequestId,fee=fee.Result});
            }
            Assert.IsFalse(reader.IsProviderActivated);Assert.IsTrue(debit>0&&debit/100000000m<=original.NativeFeeExposureUsd);
            var after=await store.GetAsync(original.Id,deadline.Token);Assert.AreEqual(before.GetRawText(),JsonSerializer.SerializeToElement(after).GetRawText());
            Assert.AreEqual(journalHash,Hash(journal));
            var report=Path.Combine(root,"TestResults/HederaRuntime/routed-activation-original-fees.json");
            using(var file=new FileStream(report,FileMode.Create,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(file,new{originalJournalSha256=journalHash,original,fees,totalDebitTinybars=debit,
                    readOnly=true,signingKeyLoaded=false,paidSdkQueries=0,nativeReplay=false,settlementAuthorized=false},cancellationToken:deadline.Token);file.Flush(true);
            }
            Console.WriteLine($"Verified4 original activation payments; actual debit={debit}tinybar; no replay or ledger mutation. Evidence SHA256={Hash(report)}");
        }finally{await reader.DeActivateProviderAsync();}
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
        =>await RunRetainedNativeRead(false);
    [TestMethod]
    public async Task RetainedTestnetRoutedReadUsesRealV2ActivationAndOriginalFees()
        =>await RunRetainedNativeRead(true);
    private async Task RunRetainedNativeRead(bool routed)
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
        // Empty private-store activation needs3authority +3revision/page/revision queries,
        // then1root read. Existing encrypted records can require more: budget must reject them explicitly.
        var maximumTestHbar=routed?.75m:.2m;var expectedPayments=routed?7:1;
        var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(token);string? operationId=null;
        var provider=new HashgraphOASIS("https://testnet.mirrornode.hedera.com","testnet","296",storageConfiguration:new(){
            ContractId="0.0.11017885",OperatorAccountId="0.0.10993779",SigningKeyType="der",SigningKeyResolver=()=>key!,
            EncryptionKeyResolver=()=>SHA256.HashData(Encoding.UTF8.GetBytes("oasis-hedera-testnet-storage-fixture-v1")),GasLimit=100000,MaxFeeTinybars=1000000,
            QueryPreparation=new(){AuthorizedNodeAccountIds=Enumerable.Range(3,7).Select(number=>"0.0."+number).ToArray(),MaxPaymentTinybars=15000000,MaxTransactionFeeTinybars=1000000,MaximumRequestsPerInvocation=routed?3:1}});
        try{
            var user=Guid.NewGuid().ToString("D");using var transport=new HttpClient(new Authority(user));
            var execution=await new Web4UsageExecution(new Web4SubscriptionAuthorizationClient(transport,"https://controlled-authority.invalid","WEB6","test-only"),store,new(){Service="WEB6"})
                .BeginAsync("test-only",user,"retained-native-read",new string('a',64),"GET /native-read-fixture",new(){MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=maximumTestHbar,MaximumNativeFeeCostUsd=maximumTestHbar,MaximumProviderAttempts=expectedPayments},token);
            operationId=execution.OperationId;var time=DateTime.UtcNow.AddSeconds(-1);
            execution=execution.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(operationId),"HashgraphOASIS","testnet","tinybar","hedera-hashgraph",8,1m,
                "synthetic-testnet-price","fixture-v1","fixture-v1",time,time,time,time.AddMinutes(5))});
            var holonId=Guid.NewGuid();await JsonSerializer.SerializeAsync(file,new{database,operationId,holonId,contractId="0.0.11017885",payer="0.0.10993779",maximumTestHbar,routed,
                recovery="Read original query payments only; never retry an uncertain paid read or remove this fence without reconciliation."},cancellationToken:token);file.Flush(true);
            NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> read;
            if(routed){
                var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA();dna.OASIS.StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings();dna.OASIS.HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2;dna.OASIS.SubscriptionConfig.PlanType="Gold";
                var manager=new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
                manager.RegisterProvider(provider);var router=new NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
                var prepared=new PreparedStorageExecution(execution,"native-v2-read",new Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType,string>{{NextGenSoftware.OASIS.API.Core.Enums.ProviderType.HashgraphOASIS,"0.0.10993779"}},Array.Empty<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>());
                read=await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(new NextGenSoftware.OASIS.API.Core.StorageOperationRequest{Operation="LoadHolon",HolonId=holonId,LoadChildren=false,
                    PreferredProvider=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.HashgraphOASIS,PreparedExecution=prepared});
                Assert.IsTrue(provider.IsProviderActivated,read.Message);Assert.AreEqual(1,manager.PerformanceMonitor.GetMetrics(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.HashgraphOASIS).TotalRequests);
            }else read=await provider.ExecutePreparedStorageAsync(()=>provider.LoadHolonAsync(holonId,loadChildren:false),execution,"native-read","0.0.10993779",token);
            Assert.IsNull(read.Result);Assert.IsTrue(!read.IsError||read.ErrorCode==NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,read.Message);
            var operation=await store.GetAsync(operationId,token);Assert.AreEqual(expectedPayments,operation.ProviderAttempts.Count);
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
            Assert.IsTrue(maximum>0&&maximum<=maximumTestHbar);Assert.AreEqual(maximum,operation.NativeFeeExposureUsd);
            var owner=new PreparedNativeUsageRecoveryResolver(store,new[]{(NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader)provider});
            Assert.IsNull(await owner.ResolveAsync(operation,token));var measured=await store.GetAsync(operationId,token);Assert.AreEqual(operation.ProviderAttempts.Count,measured.Receipts.Count);Assert.IsNull(measured.Settlement);
            Console.WriteLine(JsonSerializer.Serialize(new{originals,measured.Receipts,maximumTestHbar,routed,measured.NativeFeeExposureUsd,hostedSettlementVerified=false,storageMutations=0}));
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
