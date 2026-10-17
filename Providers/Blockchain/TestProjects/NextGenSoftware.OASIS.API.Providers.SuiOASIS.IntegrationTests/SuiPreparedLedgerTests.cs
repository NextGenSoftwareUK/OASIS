using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class SuiPreparedLedgerTests
{
    [TestMethod]
    public async Task InspectRetainedPreparedAttemptsWithoutMutation()
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/start-sui-test-network.sh")))directory=directory.Parent;
        Assert.IsNotNull(directory);
        var journal=Path.Combine(directory!.FullName,"TestResults/SuiCurrent/prepared-live-pending.json");
        var original=File.ReadAllBytes(journal);using var intent=JsonDocument.Parse(original);
        Assert.AreEqual("usage_sui_live_prepared",intent.RootElement.GetProperty("database").GetString());
        var store=new MongoUsageOutboxStore(connection,"usage_sui_live_prepared");
        var operation=await store.GetAsync(intent.RootElement.GetProperty("operationId").GetString()!,CancellationToken.None);
        Assert.IsNotNull(operation);Assert.AreEqual(3,operation.ProviderAttempts.Count);
        CollectionAssert.AreEqual(new[]{"write.storage.1","write.storage.2","cleanup.storage.1"},operation.ProviderAttempts.Select(x=>x.ClientRequestId).ToArray());
        Assert.IsTrue(operation.ProviderAttempts.All(x=>x.Provider=="SuiOASIS"&&!string.IsNullOrWhiteSpace(x.ProviderRequestId)));
        Assert.IsFalse(operation.ProviderAttempts.Any(x=>x.ClientRequestId.StartsWith("private-")));
        var chainOutcomeConfirmed=false;
        var nativeFees=new List<object>();
        if(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC") is string rpc){
            Assert.IsTrue(new Uri(rpc).IsLoopback);
            using var deployment=JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")!));
            var settings=deployment.RootElement;
            Assert.AreEqual(intent.RootElement.GetProperty("chainId").GetString(),settings.GetProperty("chainId").GetString());
            using var provider=new SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,
                settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!,
                storageEncryptionKeyResolver:()=>System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("oasis-sui-prepared-storage-fixture-v1")));
            foreach(var attempt in operation.ProviderAttempts){
                var fee=await provider.GetPreparedNativeFeeReceiptAsync(attempt);
                Assert.IsFalse(fee.IsError,fee.Message);Assert.IsTrue(fee.Result.GetProperty("success").GetBoolean());
                Assert.AreEqual(attempt.ProviderRequestId,fee.Result.GetProperty("transactionHash").GetString());
                Assert.AreEqual(settings.GetProperty("chainId").GetString(),fee.Result.GetProperty("chainIdentifier").GetString());
                Assert.AreEqual(settings.GetProperty("address").GetString(),fee.Result.GetProperty("payer").GetString());
                var netUnits=fee.Result.GetProperty("netUnits").GetString();
                Assert.IsFalse(string.IsNullOrWhiteSpace(netUnits));
                nativeFees.Add(new {attempt.ClientRequestId,attempt.ProviderRequestId,netUnits});
            }
            var sdk=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
            var raw=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"get",new{key=intent.RootElement.GetProperty("key").GetString()},CancellationToken.None})!;
            Assert.AreEqual(JsonValueKind.Null,raw.ValueKind);
            var privateRead=await provider.LoadHolonAsync(intent.RootElement.GetProperty("privateHolonId").GetGuid(),loadChildren:false);
            Assert.IsNull(privateRead.Result);
            Assert.IsTrue(!privateRead.IsError||privateRead.ErrorCode==NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,privateRead.Message);
            chainOutcomeConfirmed=true;
            Console.WriteLine("Original signed attempts all confirmed successful; raw fixture deleted and private holon absent. Journal retained.");
        }
        Console.WriteLine(JsonSerializer.Serialize(new{operationId=operation.Id,attempts=operation.ProviderAttempts.Select(x=>new{x.ClientRequestId,x.ProviderRequestId}),
            operation.State,operation.Service,operation.Endpoint,recordedReceiptCount=operation.Receipts.Count,
            settlementPresent=operation.Settlement!=null,nativeFees,
            chainOutcomeConfirmed,reconciliationComplete=false,reason=chainOutcomeConfirmed
                ? "Original attempts confirmed successful and fixture absence verified; durable operation settlement/ownership reconciliation still required. Journal retained."
                : "Original identities only; chain terminal status/readback still required."}));
        CollectionAssert.AreEqual(original,File.ReadAllBytes(journal));
    }
    [TestMethod]
    public async Task ReconcileControlledMintOwnerFixture()
    {
        const string connection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1"))) directory = directory.Parent;
        Assert.IsNotNull(directory);
        var journal = Path.Combine(directory!.FullName, "TestResults/SubscriptionUsageCurrent/sui-mint-owner-pending.json");
        using var claim = new FileStream(journal, FileMode.Open, FileAccess.Read, FileShare.None);
        using var document = await JsonDocument.ParseAsync(claim);
        var evidence = document.RootElement;
        const string database = "usage_sui_mint_owner_fixture";
        Assert.AreEqual(database, evidence.GetProperty("database").GetString());
        Assert.AreEqual("Controlled SDK evidence only. Preserve and inspect this outbox; never use these fixture bytes on a real chain.", evidence.GetProperty("recovery").GetString());
        var id = evidence.GetProperty("operationId").GetString()!;
        Assert.IsTrue(Guid.TryParse(id, out _));
        var mongo = new MongoDB.Driver.MongoClient(connection);
        var collection = mongo.GetDatabase(database).GetCollection<UsageOutboxOperation>("subscription_usage_outbox_v1");
        Assert.AreEqual(1L, await collection.CountDocumentsAsync(MongoDB.Driver.FilterDefinition<UsageOutboxOperation>.Empty));
        var operation = await new MongoUsageOutboxStore(connection, database).GetAsync(id, default);
        Assert.IsNotNull(operation);
        Assert.IsNull(operation.Settlement);
        Assert.AreEqual(1, operation.ProviderAttempts.Count);
        var attempt = operation.ProviderAttempts.Single();
        Assert.IsTrue(attempt.Provider == "SuiOASIS" || attempt.Provider == "CosmosBlockChainOASIS");
        Assert.AreEqual(attempt.Provider == "SuiOASIS" ? new string('1',32) : new string('a',64), attempt.ProviderRequestId);
        Assert.AreEqual("fixture-chain", attempt.NativePreparation.ChainId);
        Assert.AreEqual("fixture-owner", attempt.NativePreparation.Sender);
        Assert.AreEqual("AQID", attempt.NativePreparation.SignedTransactionBase64);
        Assert.IsNull(attempt.AcknowledgedAtUtc);
        var report = Path.Combine(directory.FullName, "TestResults/SubscriptionUsageCurrent/controlled-mint-owner-reconciled.json");
        using (var archive = new FileStream(report, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(archive, new { operation.Id, operation.State, attempt.Provider,
                attempt.ProviderRequestId, cleanup = "Verified controlled SDK fixture; no real chain submission or billing settlement." });
            archive.Flush(true);
        }
        await mongo.DropDatabaseAsync(database);
        claim.Dispose();
        File.Delete(journal);
    }
    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public async Task PublicMintRetainsOriginalAttemptInReusableMongo(bool cosmos, bool offChain)
    {
        // Real durable outbox, controlled SDK/authority: no chain or fee claim.
        const string connection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1"))) directory = directory.Parent;
        Assert.IsNotNull(directory);
        var journal = Path.Combine(directory!.FullName, "TestResults/SubscriptionUsageCurrent/sui-mint-owner-pending.json");
        const string database = "usage_sui_mint_owner_fixture";
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var bridgeVariable = cosmos ? "OASIS_COSMOS_SDK_BRIDGE" : "OASIS_SUI_SDK_BRIDGE";
        var previous = Environment.GetEnvironmentVariable(bridgeVariable);
        var complete = false;
        using var journalFile = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        var mongo = new MongoDB.Driver.MongoClient(connection);
        try
        {
            using (var databases = await mongo.ListDatabaseNamesAsync(cancellationToken: deadline.Token))
            {
                var names = await MongoDB.Driver.IAsyncCursorExtensions.ToListAsync(databases, deadline.Token);
                Assert.IsFalse(names.Contains(database), "Existing fixture database requires reconciliation; never overwrite or delete it.");
            }
            var store = new MongoUsageOutboxStore(connection, database);
            await store.InitializeAsync(deadline.Token);
            var user = Guid.NewGuid().ToString("D");
            using var http = new HttpClient(new TestAuthority(user));
            var authority = new Web4SubscriptionAuthorizationClient(http, "https://test-authority.invalid", "WEB6", "test-key");
            var execution = await new Web4UsageExecution(authority, store, new Web4UsageLedgerOptions { Service = "WEB6", PersistenceTimeout = TimeSpan.FromSeconds(5) })
                .BeginAsync("test-only", user, "sui-mint-owner-fixture", new string('a', 64), "POST /controlled-mint-test",
                    new UsageEndpointPolicy { MeterCategory = "api.request", ReservedUnits = 1 }, deadline.Token);
            await JsonSerializer.SerializeAsync(journalFile, new { database, operationId = execution.OperationId,
                recovery = "Controlled SDK evidence only. Preserve and inspect this outbox; never use these fixture bytes on a real chain." }, cancellationToken: deadline.Token);
            journalFile.Flush(true);
            Environment.SetEnvironmentVariable(bridgeVariable, Path.Combine(directory.FullName, cosmos
                ? "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests/Fixtures/nft-signer.mjs"
                : "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests/Fixtures/unlock-readback-failure.mjs"));
            using var provider = new SuiOASIS("http://127.0.0.1:1", "localnet", "fixture-chain", privateKey: "test-only-secret");
            using var cosmosProvider = new CosmosBlockChainOASIS.CosmosBlockChainOASIS("http://127.0.0.1:1", "fixture-chain", "test-only-explicit", "fixture-contract");
            var request = new Core.Objects.NFT.Requests.MintWeb3NFTRequest { Title = offChain ? "private-canary" : "identity/valid", NumberToMint = cosmos ? 1 : 2,
                Price = 12.5m, Discount = 2m, RoyaltyPercentage = 7, MemoText = "mint memo",
                StoreNFTMetaDataOnChain = !offChain, JSONMetaDataURL = offChain ? "https://metadata.example/nft.json" : null,
                JSONMetaData = offChain ? "{\"private-canary\":true}" : null,
                MetaData = new Dictionary<string,string> { ["Email"] = "private-canary" } };
            var minted = cosmos ? await cosmosProvider.MintNFTAsync(request, execution, "mint", deadline.Token)
                : await provider.MintNFTAsync(request, execution, "mint", deadline.Token);
            Assert.IsFalse(minted.IsError, minted.Message);
            Assert.AreEqual(!offChain, minted.Result.Web3NFT.StoreNFTMetaDataOnChain);
            Assert.AreEqual(request.Title, minted.Result.Web3NFT.Title);
            if (offChain)
            {
                Assert.AreEqual(request.JSONMetaDataURL, minted.Result.Web3NFT.JSONMetaDataURL);
                Assert.AreEqual(request.JSONMetaData, minted.Result.Web3NFT.JSONMetaData);
            }
            var reopened = new MongoUsageOutboxStore(connection, database);
            var retained = await reopened.GetAsync(execution.OperationId, deadline.Token);
            Assert.AreEqual(1, retained.ProviderAttempts.Count);
            var attempt = retained.ProviderAttempts.Single();
            Assert.AreEqual(cosmos ? "CosmosBlockChainOASIS" : "SuiOASIS", attempt.Provider);
            Assert.AreEqual("mint", attempt.ClientRequestId);
            Assert.AreEqual(minted.Result.TransactionResult, attempt.ProviderRequestId);
            Assert.AreEqual("fixture-chain", attempt.NativePreparation.ChainId);
            Assert.AreEqual("fixture-owner", attempt.NativePreparation.Sender);
            Assert.AreEqual("AQID", attempt.NativePreparation.SignedTransactionBase64);
            Assert.IsNull(attempt.AcknowledgedAtUtc);
            Assert.IsNull(retained.Settlement);
            var duplicate = cosmos ? await cosmosProvider.MintNFTAsync(request, execution, "mint", deadline.Token)
                : await provider.MintNFTAsync(request, execution, "mint", deadline.Token);
            Assert.IsTrue(duplicate.IsError, "Same owned attempt must not be submitted again.");
            Assert.IsInstanceOfType<UsageProtocolException>(duplicate.Exception);
            Assert.AreEqual(1, (await reopened.GetAsync(execution.OperationId, deadline.Token)).ProviderAttempts.Count);
            await mongo.DropDatabaseAsync(database, deadline.Token);
            complete = true;
        }
        finally
        {
            Environment.SetEnvironmentVariable(bridgeVariable, previous);
            journalFile.Dispose();
            if (complete) File.Delete(journal);
        }
    }
    [TestMethod]
    public async Task ExistingOriginalFeeReceiptBindsActualPayerAndExactComponents()
    {
        var rpc=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC")??throw new InvalidOperationException("Retained RPC required.");Assert.IsTrue(new Uri(rpc).IsLoopback);
        var hash=Environment.GetEnvironmentVariable("OASIS_SUI_SUCCESSFUL_FEE_RECEIPT")??throw new InvalidOperationException("Existing original digest required; do not mint a new receipt fixture.");
        using var deployment=JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")??throw new InvalidOperationException("Retained deployment required.")));
        var settings=deployment.RootElement;
        using var provider=new SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,
            settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!,
            storageEncryptionKeyResolver:()=>System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("oasis-sui-prepared-storage-fixture-v1")));
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result=await provider.GetNativeFeeReceiptAsync(hash,deadline.Token);Assert.IsFalse(result.IsError,result.Message);
        var receipt=result.Result;Assert.AreEqual(hash,receipt.GetProperty("transactionHash").GetString());Assert.IsTrue(receipt.GetProperty("success").GetBoolean());
        Assert.AreEqual(settings.GetProperty("chainId").GetString(),receipt.GetProperty("chainIdentifier").GetString());
        Assert.AreEqual(settings.GetProperty("address").GetString(),receipt.GetProperty("payer").GetString());
        var components=receipt.GetProperty("components");
        var expected=System.Numerics.BigInteger.Parse(components.GetProperty("computationCost").GetString()!)
            +System.Numerics.BigInteger.Parse(components.GetProperty("storageCost").GetString()!)
            -System.Numerics.BigInteger.Parse(components.GetProperty("storageRebate").GetString()!);
        Assert.AreEqual(expected,System.Numerics.BigInteger.Parse(receipt.GetProperty("netUnits").GetString()!));
        Console.WriteLine($"Original native fee verified: digest={hash}; netMIST={expected}; no submission.");
    }
    [TestMethod]
    public async Task ActivationVerifiesRealOwnerWithoutNetworkFee()
    {
        var rpc=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC")??throw new InvalidOperationException("Retained RPC required.");
        Assert.IsTrue(new Uri(rpc).IsLoopback);
        using var deployment=JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")??throw new InvalidOperationException("Retained deployment required.")));
        var settings=deployment.RootElement;
        using var provider=new SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,
            settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var address=settings.GetProperty("address").GetString()!;
        var before=await provider.GetAccountBalanceAsync(address,deadline.Token);Assert.IsFalse(before.IsError,before.Message);
        try{
            var activated=await provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);Assert.IsTrue(activated.Result);
            var after=await provider.GetAccountBalanceAsync(address,deadline.Token);Assert.IsFalse(after.IsError,after.Message);
            Assert.AreEqual(before.Result,after.Result,"Read-only activation must not charge a network mutation fee.");
            var sdk=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            var other=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"generateKey",null!,deadline.Token})!;
            using var foreign=new SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,
                other.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!);
            var rejected=await foreign.ActivateProviderAsync();Assert.IsTrue(rejected.IsError,"Foreign signer must not activate owned storage.");
            Assert.IsFalse(foreign.IsProviderActivated);StringAssert.Contains(rejected.Message,"owner does not match");
            var unchanged=await provider.GetAccountBalanceAsync(address,deadline.Token);Assert.IsFalse(unchanged.IsError,unchanged.Message);
            Assert.AreEqual(before.Result,unchanged.Result,"Rejected foreign authority must not trigger a paid activation probe.");
        }finally{await provider.DeActivateProviderAsync();}
    }
    private sealed class TestAuthority(string user):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var body=await request.Content!.ReadAsStringAsync(token);object response;
            if(request.RequestUri!.AbsolutePath.EndsWith("authorize")){
                var input=JsonSerializer.Deserialize<UsageAuthorizationRequest>(body)!;
                response=new SubscriptionAuthorizationResult{Allowed=true,StatusCode=200,UserId=user,OperationId=input.OperationId,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(2)};
            }else if(request.RequestUri.AbsolutePath.EndsWith("start")){
                var input=JsonSerializer.Deserialize<UsageStartRequest>(body)!;
                response=new UsageStartResult{Started=true,OperationId=input.OperationId,Status="executing"};
            }else throw new InvalidOperationException("Unexpected test authority call.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(response),System.Text.Encoding.UTF8,"application/json")};
        }
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task PreparedLocalWriteRetainsOriginalSignedEvidenceAndConfirmsLedger(bool loseConfirmedReply,bool storageScope=false)
    {
        var rpc=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC")??throw new InvalidOperationException("Retained RPC required.");
        Assert.IsTrue(new Uri(rpc).IsLoopback,"Never submit this fixture to a public chain.");
        using var deployment=JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")??throw new InvalidOperationException("Retained deployment required.")));
        var settings=deployment.RootElement;
        using var provider=new SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,
            settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!,
            storageEncryptionKeyResolver:()=>System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("oasis-sui-prepared-storage-fixture-v1")));
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"),
            "Use the retained owned test service; never fall back to the user's Mongo instance.");
        const string database="usage_sui_live_prepared";
        var mongo=new MongoDB.Driver.MongoClient(connection);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var key="oasis/prepared-test/"+Guid.NewGuid().ToString("N");
        var privateHolonId=Guid.NewGuid();
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/start-sui-test-network.sh")))directory=directory.Parent;
        Assert.IsNotNull(directory,"Existing task repository required.");
        var journal=Path.Combine(directory!.FullName,"TestResults/SuiCurrent/prepared-live-pending.json");
        Directory.CreateDirectory(Path.GetDirectoryName(journal)!);
        Assert.IsFalse(File.Exists(journal),"Existing prepared intent must be reconciled, not replaced.");
        using(var databases=await mongo.ListDatabaseNamesAsync(cancellationToken:deadline.Token))
            Assert.IsFalse((await MongoDB.Driver.IAsyncCursorExtensions.ToListAsync(databases,deadline.Token)).Contains(database),
                "Retained prepared database requires inspection/reconciliation before reuse; do not drop it blindly.");
        // CreateNew is an atomic fence: never repeat a previous unknown submission.
        using var journalFile=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        try{
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new TestAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://test-authority.invalid","WEB6","test-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only",user,key,new string('a',64),"POST /local-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            var sdk=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            Task<JsonElement> Mutate(string operation,object args,string correlation)=>storageScope
                ? provider.ExecutePreparedStorageAsync(async()=>{
                    var response=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{operation,args,deadline.Token})!;
                    if(operation=="put"&&correlation=="write"&&!loseConfirmedReply)
                        response=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{operation,args,deadline.Token})!;
                    return response;
                },execution,correlation,
                    settings.GetProperty("address").GetString()!,deadline.Token)
                : provider.ExecutePreparedNativeAsync(operation,args,execution,correlation,settings.GetProperty("chainId").GetString()!,settings.GetProperty("address").GetString()!,deadline.Token);
            await JsonSerializer.SerializeAsync(journalFile,new{database,key,storageScope,privateHolonId,encryptionKeyLabel="oasis-sui-prepared-storage-fixture-v1",operationId=execution.OperationId,chainId=settings.GetProperty("chainId").GetString(),
                recovery="Inspect original write and cleanup attempts in this retained Mongo outbox; never resubmit on unknown outcome."},cancellationToken:deadline.Token);
            journalFile.Flush(true);
            string hash;
            if(loseConfirmedReply){
                var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
                var previousReal=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_REAL_BRIDGE");
                Assert.IsFalse(string.IsNullOrWhiteSpace(previous),"Retained source SDK path required.");
                try{
                    Environment.SetEnvironmentVariable("OASIS_SUI_TEST_REAL_BRIDGE",previous);
                    Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures/lose-confirmed-reply.mjs"));
                    var failure=await Assert.ThrowsExceptionAsync<SuiNativeMutationReconciliationException>(()=>Mutate("put",new{key,value="owned prepared live fixture"},"write"));
                    hash=failure.OriginalTransactionId;Assert.IsFalse(string.IsNullOrWhiteSpace(hash));
                }finally{
                    Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);
                    Environment.SetEnvironmentVariable("OASIS_SUI_TEST_REAL_BRIDGE",previousReal);
                }
            }else{
                var response=await Mutate("put",new{key,value="owned prepared live fixture"},"write");
                hash=response.GetProperty("transactionHash").GetString()!;
            }
            var stored=await store.GetAsync(execution.OperationId,deadline.Token);
            var expectedWrites=storageScope&&!loseConfirmedReply?2:1;
            Assert.AreEqual(expectedWrites,stored.ProviderAttempts.Count);
            for(var index=0;index<expectedWrites;index++)
                Assert.AreEqual(storageScope?"write.storage."+(index+1):"write",stored.ProviderAttempts[index].ClientRequestId);
            if(expectedWrites==2)Assert.AreNotEqual(stored.ProviderAttempts[0].ProviderRequestId,stored.ProviderAttempts[1].ProviderRequestId);
            var attempt=stored.ProviderAttempts[expectedWrites-1];Assert.AreEqual(hash,attempt.ProviderRequestId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(attempt.NativePreparation.SignedTransactionBase64));
            Assert.IsFalse(string.IsNullOrWhiteSpace(attempt.NativePreparation.Signature));
            Assert.IsNull(attempt.AcknowledgedAtUtc,"Preparation alone must not claim ledger settlement.");
            var recoveredFee=await provider.GetPreparedNativeFeeReceiptAsync(attempt,deadline.Token);
            Assert.IsFalse(recoveredFee.IsError,recoveredFee.Message);
            Assert.AreEqual(hash,recoveredFee.Result.GetProperty("transactionHash").GetString());
            Assert.AreEqual(settings.GetProperty("chainId").GetString(),recoveredFee.Result.GetProperty("chainIdentifier").GetString());
            var status=await provider.GetTransactionStatusAsync(hash,deadline.Token);Assert.IsFalse(status.IsError,status.Message);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,status.Result);
            var readback=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"get",new{key},deadline.Token})!;
            Assert.AreEqual("owned prepared live fixture",readback.GetString());
            var conflict=await Assert.ThrowsExceptionAsync<UsageProtocolException>(()=>Mutate("put",new{key,value="must never be submitted"},"write"));
            Assert.AreEqual("PROVIDER_ATTEMPT_ALREADY_STARTED",conflict.Code);
            var afterDuplicate=await store.GetAsync(execution.OperationId,deadline.Token);
            Assert.AreEqual(expectedWrites,afterDuplicate.ProviderAttempts.Count);Assert.AreEqual(stored.NativePreparationBytes,afterDuplicate.NativePreparationBytes);
            var unchanged=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"get",new{key},deadline.Token})!;
            Assert.AreEqual("owned prepared live fixture",unchanged.GetString());
            var cleanup=await Mutate("delete",new{key},"cleanup");
            var cleanupHash=cleanup.GetProperty("transactionHash").GetString()!;
            var cleaned=await store.GetAsync(execution.OperationId,deadline.Token);Assert.AreEqual(expectedWrites+1,cleaned.ProviderAttempts.Count);
            Assert.AreEqual(cleanupHash,cleaned.ProviderAttempts[expectedWrites].ProviderRequestId);
            if(storageScope&&!loseConfirmedReply){
                var holon=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=privateHolonId,Name="owned encrypted prepared holon"};
                var saved=await provider.ExecutePreparedStorageAsync(()=>provider.SaveHolonAsync(holon),execution,"private-save",
                    settings.GetProperty("address").GetString()!,deadline.Token);
                Assert.IsFalse(saved.IsError,saved.Message);
                var persisted=await store.GetAsync(execution.OperationId,deadline.Token);
                Assert.IsTrue(persisted.ProviderAttempts.Count>cleaned.ProviderAttempts.Count,"Encrypted public save must persist physical write attempts.");
                var read=await provider.LoadHolonAsync(privateHolonId,loadChildren:false);
                Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(holon.Name,read.Result.Name);
                var removed=await provider.ExecutePreparedStorageAsync(()=>provider.DeleteHolonAsync(privateHolonId),execution,"private-delete",
                    settings.GetProperty("address").GetString()!,deadline.Token);
                Assert.IsFalse(removed.IsError,removed.Message);
                var missing=await provider.LoadHolonAsync(privateHolonId,loadChildren:false);Assert.IsNull(missing.Result);
                cleaned=await store.GetAsync(execution.OperationId,deadline.Token);
                Assert.IsTrue(cleaned.ProviderAttempts.Count>persisted.ProviderAttempts.Count,"Public delete must also be prepared.");
            }
            foreach(var physicalAttempt in cleaned.ProviderAttempts){
                var physicalFee=await provider.GetPreparedNativeFeeReceiptAsync(physicalAttempt,deadline.Token);
                Assert.IsFalse(physicalFee.IsError,physicalFee.Message);
                Assert.AreEqual(physicalAttempt.ProviderRequestId,physicalFee.Result.GetProperty("transactionHash").GetString());
                Assert.AreEqual(settings.GetProperty("chainId").GetString(),physicalFee.Result.GetProperty("chainIdentifier").GetString());
                Assert.AreEqual(settings.GetProperty("address").GetString(),physicalFee.Result.GetProperty("payer").GetString());
                Assert.IsTrue(physicalFee.Result.GetProperty("success").GetBoolean());
                Assert.IsFalse(string.IsNullOrWhiteSpace(physicalFee.Result.GetProperty("netUnits").GetString()));
            }
            var cleanupStatus=await provider.GetTransactionStatusAsync(cleanupHash,deadline.Token);Assert.IsFalse(cleanupStatus.IsError,cleanupStatus.Message);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,cleanupStatus.Result);
            var absent=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"get",new{key},deadline.Token})!;
            Assert.AreEqual(JsonValueKind.Null,absent.ValueKind);
            Console.WriteLine($"Confirmed original prepared write={hash}; cleanup={cleanupHash}; owned key={key}.");
            await mongo.DropDatabaseAsync(database);
            journalFile.Dispose();File.Delete(journal);
        }catch{
            Console.WriteLine($"Retained original prepared evidence for reconciliation: {journal}; database={database}; key={key}.");
            throw;
        }
    }
}
