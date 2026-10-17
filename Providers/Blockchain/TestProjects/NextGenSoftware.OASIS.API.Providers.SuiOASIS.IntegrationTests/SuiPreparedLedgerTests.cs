using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class SuiPreparedLedgerTests
{
    [TestMethod]
    public async Task InspectRetainedPreparedAttemptsWithoutMutation()
        =>await InspectRetainedPreparedAttempts(false);
    [TestMethod]
    public async Task ArchiveConfirmedRetainedLocalFixtureWithoutResubmission()
        =>await InspectRetainedPreparedAttempts(true);
    private async Task InspectRetainedPreparedAttempts(bool archiveFixture)
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/start-sui-test-network.sh")))directory=directory.Parent;
        Assert.IsNotNull(directory);
        var journal=Path.Combine(directory!.FullName,"TestResults/SuiCurrent/prepared-live-pending.json");
        var original=File.ReadAllBytes(journal);using var intent=JsonDocument.Parse(original);
        if(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(original))=="D5DE1EC760F1E9F7DD19FC1ED66B592AD5CC5917A88A65A918241DAE2CACC354"){
            await InspectRoutingFixtureAsync(connection,intent.RootElement,archiveFixture,journal,original);
            if(!archiveFixture)CollectionAssert.AreEqual(original,File.ReadAllBytes(journal));return;
        }
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
        if(archiveFixture){
            Assert.IsTrue(chainOutcomeConfirmed,"Never retire fixture ownership without fresh native confirmation/readback.");
            Assert.AreEqual("B7E6EECC7C32EF5AD030C740ECF1071CB54FF05905FF69FC8DBE04A759061599",
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(original)),"Only this reviewed original fixture may be archived.");
            Assert.AreEqual("6b9932e3-f624-2f2e-f87e-b3784488cbce",operation.Id);
            Assert.AreEqual("WEB6",operation.Service);Assert.AreEqual("POST /local-test",operation.Endpoint);
            Assert.IsNull(operation.Settlement);Assert.AreEqual(0,operation.Receipts.Count);
            Assert.AreEqual("non-billable-v1",operation.Policy.PricingCatalogueVersion);
            Assert.AreEqual(0m,operation.Policy.FixedCostUsd);Assert.AreEqual(0m,operation.Policy.EstimatedCostUsd);
            Assert.IsFalse(operation.Policy.RequiresProviderMeasurement);
            var mongo=new MongoDB.Driver.MongoClient(connection);var database=mongo.GetDatabase("usage_sui_live_prepared");
            using(var collections=await database.ListCollectionNamesAsync())
                CollectionAssert.AreEquivalent(new[]{"subscription_usage_outbox_v1"},await MongoDB.Driver.IAsyncCursorExtensions.ToListAsync(collections));
            var rows=database.GetCollection<UsageOutboxOperation>("subscription_usage_outbox_v1");
            Assert.AreEqual(1L,await rows.CountDocumentsAsync(MongoDB.Driver.FilterDefinition<UsageOutboxOperation>.Empty));
            var archivePath=Path.Combine(directory.FullName,"TestResults/SuiCurrent/prepared-live-reconciled-original.json");
            using(var archive=new FileStream(archivePath,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(archive,new {journal=JsonSerializer.Deserialize<JsonElement>(original),operation,nativeFees,
                    chainOutcomeConfirmed,fixtureCleanup=true,productionBillingReconciled=false,
                    reason="Reviewed local synthetic-authority fixture only; original signed evidence retained, no replay or external settlement."});
                archive.Flush(true);
            }
            using(var archived=JsonDocument.Parse(File.ReadAllBytes(archivePath))){
                Assert.AreEqual(operation.Id,archived.RootElement.GetProperty("operation").GetProperty("Id").GetString());
                Assert.AreEqual(3,archived.RootElement.GetProperty("operation").GetProperty("ProviderAttempts").GetArrayLength());
            }
            // Exact disposable test database only, after fresh native receipts, absence, ownership and durable archive checks.
            await mongo.DropDatabaseAsync("usage_sui_live_prepared");
            CollectionAssert.AreEqual(original,File.ReadAllBytes(journal));
            File.Delete(journal);
            Console.WriteLine("Reviewed synthetic fixture archived with original signed attempts; exact owned test database/journal retired. No production billing settlement or native submission.");
        }
    }
    private static async Task InspectRoutingFixtureAsync(string connection,JsonElement intent,bool cleanup,string journal,byte[] original)
    {
        Assert.AreEqual("2c3ae4e5-ef34-fb7c-1711-b7c8beca1fdc",intent.GetProperty("operationId").GetString());
        var store=new MongoUsageOutboxStore(connection,"usage_sui_live_prepared");
        var operation=await store.GetAsync(intent.GetProperty("operationId").GetString()!,default);
        CollectionAssert.AreEqual(new[]{"write.storage.1","write.storage.2","cleanup.storage.1","private-routing.route.1.storage.1"},operation.ProviderAttempts.Select(attempt=>attempt.ClientRequestId).ToArray());
        using var deployment=JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")!));var settings=deployment.RootElement;
        var rpc=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC")!;Assert.IsTrue(new Uri(rpc).IsLoopback);
        Assert.AreEqual(intent.GetProperty("chainId").GetString(),settings.GetProperty("chainId").GetString());
        using var provider=new SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!,
            storageEncryptionKeyResolver:()=>System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("oasis-sui-prepared-storage-fixture-v1")));
        foreach(var attempt in operation.ProviderAttempts){
            var fee=await provider.GetPreparedNativeFeeReceiptAsync(attempt);Assert.IsFalse(fee.IsError,fee.Message);Assert.IsTrue(fee.Result.GetProperty("success").GetBoolean());
            Assert.AreEqual(attempt.ProviderRequestId,fee.Result.GetProperty("transactionHash").GetString());
            Console.WriteLine(JsonSerializer.Serialize(new{attempt.ClientRequestId,attempt.ProviderRequestId,confirmed=true}));
        }
        var read=await provider.LoadHolonAsync(intent.GetProperty("privateHolonId").GetGuid(),loadChildren:false);
        Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual("owned encrypted prepared holon",read.Result.Name);
        Console.WriteLine(JsonSerializer.Serialize(new {operation.Id,attempts=operation.ProviderAttempts.Count,ownedHolonConfirmed=true,readOnly=true}));
        if(!cleanup)return;
        var archive=Path.Combine(Path.GetDirectoryName(journal)!,"prepared-routing-reconciled-original.json");
        Assert.IsFalse(File.Exists(archive),"Never replace existing reconciliation evidence.");
        var sdk=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
        var absent=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"get",new{key=intent.GetProperty("key").GetString()},CancellationToken.None})!;
        Assert.AreEqual(JsonValueKind.Null,absent.ValueKind);
        using var http=new HttpClient(new TestAuthority(operation.UserId));
        var authority=new Web4SubscriptionAuthorizationClient(http,"https://test-authority.invalid","WEB6","test-key");
        var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"})
            .BeginAsync("test-only",operation.UserId,"reviewed-routing-hard-delete",new string('c',64),"POST /reviewed-local-cleanup",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1});
        var removed=await provider.ExecutePreparedStorageAsync(()=>provider.DeleteHolonAsync(intent.GetProperty("privateHolonId").GetGuid()),execution,"reviewed-cleanup",settings.GetProperty("address").GetString()!);
        Assert.IsFalse(removed.IsError,removed.Message);Assert.IsNotNull(removed.Result);
        var missing=await provider.LoadHolonAsync(intent.GetProperty("privateHolonId").GetGuid(),loadChildren:false);
        Assert.IsNull(missing.Result);Assert.IsTrue(!missing.IsError||missing.ErrorCode==NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound);
        var retainedCleanup=await store.GetAsync(execution.OperationId,default);Assert.AreEqual(1,retainedCleanup.ProviderAttempts.Count);
        var receipt=await provider.GetPreparedNativeFeeReceiptAsync(retainedCleanup.ProviderAttempts.Single());
        Assert.IsFalse(receipt.IsError,receipt.Message);Assert.IsTrue(receipt.Result.GetProperty("success").GetBoolean());
        var mongo=new MongoDB.Driver.MongoClient(connection);var database=mongo.GetDatabase("usage_sui_live_prepared");
        using(var collections=await database.ListCollectionNamesAsync())CollectionAssert.AreEquivalent(new[]{"subscription_usage_outbox_v1"},await MongoDB.Driver.IAsyncCursorExtensions.ToListAsync(collections));
        Assert.AreEqual(2L,await database.GetCollection<UsageOutboxOperation>("subscription_usage_outbox_v1").CountDocumentsAsync(MongoDB.Driver.FilterDefinition<UsageOutboxOperation>.Empty));
        using(var file=new FileStream(archive,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            await JsonSerializer.SerializeAsync(file,new{intent,operation,cleanup=retainedCleanup,nativeCleanupReceipt=receipt.Result,fixtureAbsent=true,productionBillingReconciled=false});file.Flush(true);
        }
        using(var verified=JsonDocument.Parse(File.ReadAllBytes(archive)))Assert.IsTrue(verified.RootElement.GetProperty("fixtureAbsent").GetBoolean());
        CollectionAssert.AreEqual(original,File.ReadAllBytes(journal));
        await mongo.DropDatabaseAsync("usage_sui_live_prepared");File.Delete(journal);
        Console.WriteLine("Exact reviewed routed fixture cleaned with one separately prepared delete; all five originals retained in archive. No replay or production settlement.");
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
    internal sealed class TestAuthority(string user):HttpMessageHandler
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
        const ulong localMaximumGasBudgetMist=1_000_000_000; // Explicit test-only ceiling, not production approval.
        SuiOASIS CreateLimited(ulong maximum)=>SuiOASIS.CreateWithNativeFeeLimits(new SuiNativeFeeLimits(maximum),rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,
            settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!,
            storageEncryptionKeyResolver:()=>System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("oasis-sui-prepared-storage-fixture-v1")));
        using var provider=CreateLimited(localMaximumGasBudgetMist);
        using var limited=CreateLimited(1);
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
                .BeginAsync("test-only",user,key,new string('a',64),"POST /local-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,
                    EstimatedCostUsd=10m,MaximumNativeFeeCostUsd=10m},deadline.Token);
            var priceTime=DateTime.UtcNow.AddSeconds(-1);
            var price=new NativeFeePriceQuote(Guid.Parse(execution.OperationId),"SuiOASIS",settings.GetProperty("chainId").GetString()!,
                "MIST","sui",9,1m,"synthetic-local-price","fixture-v1","fixture-v1",priceTime,priceTime,priceTime,priceTime.AddMinutes(5));
            execution=execution.WithNativePrices(new[]{price});
            var rejectedExecution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only",user,key+"-zero-budget",new string('b',64),"POST /local-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,
                    EstimatedCostUsd=1m,MaximumNativeFeeCostUsd=0m},deadline.Token);
            rejectedExecution=rejectedExecution.WithNativePrices(new[]{price with{OperationId=Guid.Parse(rejectedExecution.OperationId)}});
            var sdk=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            if(storageScope){
                var entered=false;
                foreach(var length in new[]{110,128})
                    await Assert.ThrowsExceptionAsync<ArgumentException>(()=>provider.ExecutePreparedStorageAsync(
                        ()=>{entered=true;return Task.FromResult(true);},execution,new string('x',length),
                        settings.GetProperty("address").GetString()!,deadline.Token));
                Assert.IsFalse(entered,"Invalid correlation must fail before entering the storage operation.");
                Assert.AreEqual(0,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count);
                Assert.IsTrue(await provider.ExecutePreparedStorageAsync(()=>Task.FromResult(true),execution,new string('x',109),
                    settings.GetProperty("address").GetString()!,deadline.Token));
            }
            Task<JsonElement> Mutate(string operation,object args,string correlation)=>storageScope
                ? provider.ExecutePreparedStorageAsync(async()=>{
                    var response=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{operation,args,deadline.Token})!;
                    if(operation=="put"&&correlation=="write"&&!loseConfirmedReply)
                        response=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{operation,args,deadline.Token})!;
                    return response;
                },execution,correlation,
                    settings.GetProperty("address").GetString()!,deadline.Token)
                : provider.ExecutePreparedNativeAsync(operation,args,execution,correlation,settings.GetProperty("chainId").GetString()!,settings.GetProperty("address").GetString()!,deadline.Token);
            await JsonSerializer.SerializeAsync(journalFile,new{database,key,storageScope,privateHolonId,encryptionKeyLabel="oasis-sui-prepared-storage-fixture-v1",operationId=execution.OperationId,rejectedOperationId=rejectedExecution.OperationId,chainId=settings.GetProperty("chainId").GetString(),
                recovery="Inspect original write and cleanup attempts in this retained Mongo outbox; never resubmit on unknown outcome."},cancellationToken:deadline.Token);
            journalFile.Flush(true);
            var capError=await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>limited.ExecutePreparedStorageAsync(
                async()=>await (Task<JsonElement>)sdk.Invoke(limited,new object[]{"put",new{key,value="must not be submitted above ceiling"},deadline.Token})!,
                execution,"cap-rejection",settings.GetProperty("address").GetString()!,deadline.Token));
            StringAssert.Contains(capError.Message,"gas budget exceeds");
            Assert.IsFalse(capError is SuiNativeMutationReconciliationException);
            Assert.AreEqual(0,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count);
            var beforeWrite=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"get",new{key},deadline.Token})!;
            Assert.AreEqual(JsonValueKind.Null,beforeWrite.ValueKind);
            var aggregateError=await Assert.ThrowsExceptionAsync<UsageProtocolException>(()=>provider.ExecutePreparedStorageAsync(
                async()=>await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"put",new{key,value="must not submit beyond aggregate budget"},deadline.Token})!,
                rejectedExecution,"aggregate-rejection",settings.GetProperty("address").GetString()!,deadline.Token));
            Assert.AreEqual("NATIVE_FEE_BUDGET_EXCEEDED",aggregateError.Code);
            var rejected=await store.GetAsync(rejectedExecution.OperationId,deadline.Token);
            Assert.AreEqual(0,rejected.ProviderAttempts.Count);Assert.AreEqual(0m,rejected.NativeFeeExposureUsd);
            Assert.AreEqual(JsonValueKind.Null,(await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"get",new{key},deadline.Token})!).ValueKind);
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
            foreach(var pricedAttempt in stored.ProviderAttempts)
                Assert.AreEqual(price,pricedAttempt.NativePreparation.PriceQuotes.Single());
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
            var valued=attempt.NativePreparation.PriceQuotes.Single().ValueOriginalReceipt(hash,recoveredFee.Result.GetProperty("netUnits").GetString()!);
            Assert.AreEqual("bound-native-v1:"+price.Fingerprint(),valued.ValuationVersion);
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
                var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
                dna.OASIS.SubscriptionConfig.PlanType="Gold";
                var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
                manager.RegisterProvider(provider);var router=new OASISHyperDrive(manager);
                var routingPolicy=new PreparedStorageExecution(execution,"private-routing",new Dictionary<ProviderType,string> {
                    [ProviderType.SuiOASIS]=settings.GetProperty("address").GetString()!},Array.Empty<ProviderType>());
                var beforePrivate=cleaned.ProviderAttempts.Count;
                await routingPolicy.RunAsync(async()=>{
                    var write=new StorageOperationRequest {Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=ProviderType.SuiOASIS};
                    var saved=await router.RouteRequestAsync<IHolon>(write);
                    Assert.IsFalse(saved.IsError,saved.Message);Assert.AreSame(routingPolicy,write.PreparedExecution);
                    Assert.IsTrue(provider.IsProviderActivated,"Real activation must complete inside admitted dispatch.");
                    var persisted=await store.GetAsync(execution.OperationId,deadline.Token);
                    Assert.IsTrue(persisted.ProviderAttempts.Count>beforePrivate,"Routed encrypted save must retain original physical attempts.");
                    var typed=new LoadHolonRequest {HolonId=privateHolonId,LoadChildren=false};
                    foreach(var read in new[]{router.RouteRequestToProvider<IHolon>(typed,ProviderType.SuiOASIS),await router.RouteRequestToProviderAsync<IHolon>(typed,ProviderType.SuiOASIS)}){
                        Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(holon.Name,read.Result.Name);
                    }
                    var removed=await router.RouteRequestAsync<IHolon>(new StorageOperationRequest {Operation="DeleteHolon",HolonId=privateHolonId,SoftDelete=false,PreferredProvider=ProviderType.SuiOASIS});
                    Assert.IsFalse(removed.IsError,removed.Message);Assert.IsNotNull(removed.Result);
                    cleaned=await store.GetAsync(execution.OperationId,deadline.Token);
                    Assert.IsTrue(cleaned.ProviderAttempts.Count>persisted.ProviderAttempts.Count,"Routed public delete must retain original preparation.");
                    return true;
                });
                Assert.IsNull(PreparedStorageExecution.Current);
                var missing=await provider.LoadHolonAsync(privateHolonId,loadChildren:false);Assert.IsNull(missing.Result);
                Assert.IsTrue(cleaned.ProviderAttempts.Skip(beforePrivate).All(attempt=>attempt.ClientRequestId.StartsWith("private-routing.route.",StringComparison.Ordinal)));
            }
            var lookupOwner=new PreparedNativeUsageRecoveryResolver(store,new[]{(NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader)provider});
            Assert.IsTrue(lookupOwner.CanResolve(cleaned));
            Assert.IsNull(await lookupOwner.ResolveAsync(cleaned,deadline.Token),"Fee lookup must not invent the unknown logical API outcome.");
            var measured=await store.GetAsync(execution.OperationId,deadline.Token);
            Assert.IsNull(measured.ObservedOutcome);Assert.AreEqual(cleaned.ProviderAttempts.Count,measured.Receipts.Count);
            Assert.IsFalse(lookupOwner.CanResolve(measured),"Recorded fees must not trigger another original receipt lookup.");
            decimal verifiedMaximumUsd=0m;
            foreach(var physicalAttempt in cleaned.ProviderAttempts){
                var original=physicalAttempt.NativePreparation;
                var verified=await (Task<JsonElement>)sdk.Invoke(limited,new object[]{"verifyNativePreparation",new{preparation=new{
                    transactionHash=physicalAttempt.ProviderRequestId,sender=original.Sender,chainId=original.ChainId,
                    signedTransactionBase64=original.SignedTransactionBase64,signature=original.Signature}},deadline.Token})!;
                var signedBudget=ulong.Parse(verified.GetProperty("gasBudgetMist").GetString()!,System.Globalization.CultureInfo.InvariantCulture);
                Assert.IsTrue(signedBudget>1&&signedBudget<=localMaximumGasBudgetMist);
                var maximum=original.FeeMaximums.Single();
                Assert.AreEqual("MIST",maximum.Denomination);Assert.AreEqual(NativeFeeMaximumCoverage.AllFees,maximum.Coverage);
                Assert.AreEqual(signedBudget.ToString(System.Globalization.CultureInfo.InvariantCulture),maximum.NativeMaximumUnits);
                verifiedMaximumUsd+=NativeFeeMaximumAccounting.ValueUsd(original.FeeMaximums,original.PriceQuotes);
                var historical=await limited.GetPreparedNativeFeeReceiptAsync(physicalAttempt,deadline.Token);
                Assert.IsFalse(historical.IsError,historical.Message);
                Assert.AreEqual(physicalAttempt.ProviderRequestId,historical.Result.GetProperty("transactionHash").GetString());
                Assert.AreEqual(price,physicalAttempt.NativePreparation.PriceQuotes.Single());
                var receipt=measured.Receipts.Single(receipt=>receipt.ProviderRequestId==physicalAttempt.ProviderRequestId);
                Assert.AreEqual("verified-sui-net-effects",receipt.CostSource);
                var valuation=receipt.NativeFeeValuations.Single();
                Assert.AreEqual(price.ValueOriginalReceipt(physicalAttempt.ProviderRequestId,valuation.NativeNetUnits),valuation);
                Console.WriteLine(JsonSerializer.Serialize(new{original=physicalAttempt.ProviderRequestId,valuation.NativeNetUnits,valuation.NetCostUsd,valuation.ValuationVersion}));
            }
            Assert.AreEqual(verifiedMaximumUsd,measured.NativeFeeExposureUsd);
            Assert.IsTrue(verifiedMaximumUsd>0m&&verifiedMaximumUsd<=10m);
            // Preserve the known invocation outcome; a confirmed transaction does not turn a lost API reply into success.
            var observed=await store.RecordObservedOutcomeAsync(execution.OperationId,cleaned.ExecutionOwner,loseConfirmedReply?"failed":"succeeded",deadline.Token);
            var settlement=await new RecordedUsageRecoveryResolver().ResolveAsync(observed,deadline.Token);
            Assert.AreEqual(UsageReceiptAccounting.OperationCost(execution.OperationId,observed.Receipts),settlement.ActualCostUsd);
            await store.EnqueueAsync(execution.OperationId,observed.ExecutionOwner,settlement,observed.EvidenceRevision,deadline.Token);
            Assert.AreEqual("pending",(await store.GetAsync(execution.OperationId,deadline.Token)).State);
            Console.WriteLine(JsonSerializer.Serialize(new{testOnlyPrice=price,logicalOutcome=settlement.Outcome,queuedCostUsd=settlement.ActualCostUsd,
                originalAttemptCount=observed.ProviderAttempts.Count,receiptCount=observed.Receipts.Count,hostedSettlementVerified=false}));
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
