using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmosRoutingInspectionTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task ZeroBudgetPostOutageDeleteCannotPersistOrBroadcast()
    {
        await InspectCurrentFailedProfileOriginalsWithoutMutation();
        var root=Environment.GetEnvironmentVariable("OASIS_HYPERDRIVE_TEST_ROOT")!;
        var journal=Path.Combine(root,"independent-routing-pending.json");
        const string journalHash="CE9E54E7E52BC2F8C734A37E4FB84E081D3AC686739C8FE8798EF37C26CE6A48";
        Assert.Equal(journalHash,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(journal))));
        using var intent=JsonDocument.Parse(File.ReadAllText(journal));
        var avatarId=intent.RootElement.GetProperty("avatarId").GetGuid();var leafId=intent.RootElement.GetProperty("holonId").GetGuid();
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        var store=new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing");
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(150));var token=deadline.Token;
        var originalBefore=JsonSerializer.Serialize(await store.GetAsync("366bd44d-a0f6-ba07-b9f4-b79f5f1d2bbe",token));
        using var cosmos=CosmosBlockChainOASIS.CreateWithNativeFeeLimits(
            CosmosNativeFeeLimits.ForReviewedStandardSdkChain(new Dictionary<string,string>{{"stake","50000"}},"oasis-cosmos-local"),
            rpcEndpoint:"http://127.0.0.1:26657",chainId:"oasis-cosmos-local",privateKey:Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_KEY")!,
            contractAddress:intent.RootElement.GetProperty("contract").GetString()!,addressPrefix:"wasm",gasPrice:"0.025stake",nativeDenom:"stake",nativeDecimals:0,
            storageEncryptionKeyResolver:()=>SHA256.HashData(Encoding.UTF8.GetBytes("oasis-cosmos-local-storage-fixture-v1")));
        var local=new LocalFileOASIS(storageDirectory:Path.Combine(root,"files"));
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
        dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        manager.RegisterProvider(cosmos);manager.RegisterProvider(local);
        Assert.False(manager.SetAndReplaceAutoFailOverListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.CosmosBlockChainOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)}).IsError);
        var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new CosmosIndependentRoutingTests.TestAuthority(user));
        var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.invalid","WEB6","test-only-key");
        var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"}).BeginAsync("test-only",user,Guid.NewGuid().ToString("D"),new string('d',64),
            "POST /zero-budget-native-probe",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=0m,MaximumNativeFeeCostUsd=0m,MaximumProviderAttempts=128},token);
        var now=DateTime.UtcNow.AddSeconds(-1);
        execution=execution.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(execution.OperationId),"CosmosBlockChainOASIS","oasis-cosmos-local","stake","local-test-stake",0,.000001m,
            "synthetic-local-price","fixture-v1","fixture-v1",now,now,now,now.AddMinutes(5))});
        var original=await store.GetAsync("366bd44d-a0f6-ba07-b9f4-b79f5f1d2bbe",token);
        var sender=original.ProviderAttempts.First(attempt=>attempt.Provider=="CosmosBlockChainOASIS").NativePreparation.Sender;
        var policy=new PreparedStorageExecution(execution,"zero-budget-probe",new Dictionary<ProviderType,string>{{ProviderType.CosmosBlockChainOASIS,sender}},new[]{ProviderType.LocalFileOASIS});
        var router=new OASISHyperDrive(manager);var outageReads=0;
        try{
            await policy.RunAsync(async()=>{
                Assert.Equal(leafId,(await cosmos.LoadHolonAsync(leafId,loadChildren:false)).Result?.Id);
                Assert.False((await cosmos.ActivateProviderAsync()).IsError);
                await CosmosIndependentRoutingTests.ControlNodeAsync("stop-owned-cosmos-test-node.sh");
                Exception? failure=null;
                try{
                    foreach(var suffix in new[]{"","ByProviderKey","ByUsername","ByEmail"})foreach(var sync in new[]{false,true}){
                        var request=new StorageOperationRequest{Operation="LoadAvatar"+suffix,AvatarId=avatarId,ProviderKey=avatarId.ToString(),Username=intent.RootElement.GetProperty("Username").GetString(),Email=intent.RootElement.GetProperty("Email").GetString(),PreferredProvider=ProviderType.CosmosBlockChainOASIS};
                        var read=sync?router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
                        Assert.False(read.IsError,read.Message);Assert.Equal(avatarId,read.Result?.Id);Assert.Equal(ProviderType.LocalFileOASIS,manager.LastFailoverDiagnostic.SelectedProvider);outageReads++;
                        if(suffix=="ByProviderKey")continue;
                        request.Operation="LoadAvatarDetail"+suffix;
                        var detail=sync?router.RouteRequest<IAvatarDetail>(request):await router.RouteRequestAsync<IAvatarDetail>(request);
                        Assert.False(detail.IsError,detail.Message);Assert.Equal(avatarId,detail.Result?.Id);Assert.Equal(ProviderType.LocalFileOASIS,manager.LastFailoverDiagnostic.SelectedProvider);outageReads++;
                    }
                }catch(Exception caught){failure=caught;}
                try{await CosmosIndependentRoutingTests.ResumeNodeAsync("http://127.0.0.1:26657","oasis-cosmos-local",token);}
                catch(Exception restoreFailure){if(failure!=null)throw new AggregateException(failure,restoreFailure);throw;}
                if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
                manager.IsAutoFailOverEnabled=false;
                var healthyRead=await cosmos.LoadHolonAsync(leafId,loadChildren:false);
                output.WriteLine(JsonSerializer.Serialize(new{postResumeReadError=healthyRead.IsError,healthyRead.Message}));
                Assert.False(healthyRead.IsError,healthyRead.Message);Assert.Equal(leafId,healthyRead.Result?.Id);
                var denied=router.RouteRequest<IHolon>(new StorageOperationRequest{Operation="DeleteHolon",HolonId=leafId,SoftDelete=false,PreferredProvider=ProviderType.CosmosBlockChainOASIS});
                output.WriteLine(JsonSerializer.Serialize(new{denied.IsError,denied.ErrorCode,denied.Message,inner=denied.InnerMessages,exception=denied.Exception?.ToString()}));
                Assert.True(denied.IsError);Assert.Contains("durable fee budget",denied.Message);
                Assert.Equal(leafId,(await cosmos.LoadHolonAsync(leafId,loadChildren:false)).Result?.Id);
                return true;
            });
            var probe=await store.GetAsync(execution.OperationId,token);
            Assert.Equal(14,outageReads);Assert.DoesNotContain(probe.ProviderAttempts,attempt=>attempt.Provider=="CosmosBlockChainOASIS");Assert.Equal(0m,probe.NativeFeeExposureUsd);
            Assert.Equal(originalBefore,JsonSerializer.Serialize(await store.GetAsync("366bd44d-a0f6-ba07-b9f4-b79f5f1d2bbe",token)));
            Assert.Equal(journalHash,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(journal))));
            output.WriteLine(JsonSerializer.Serialize(new{probeId=execution.OperationId,outageReads,nativeOriginals=0,nativeFeeExposureUsd=0m,originalLedgerUnchanged=true,originalLeafPresent=true}));
        }finally{await local.DeActivateProviderAsync();}
    }

    [Fact]
    public async Task InspectCurrentFailedProfileOriginalsWithoutMutation()
    {
        var root=Environment.GetEnvironmentVariable("OASIS_HYPERDRIVE_TEST_ROOT")!;
        var journal=Path.Combine(root,"independent-routing-pending.json");
        var hash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(journal)));
        if(hash=="F901B2959830F84882BBC338992EF734612D7009BBF318E36F178E279CE98BC1"){
            await InspectExactFailedProfileOriginalsWithoutMutation();return;
        }
        Assert.Equal("CE9E54E7E52BC2F8C734A37E4FB84E081D3AC686739C8FE8798EF37C26CE6A48",hash);
        using var intent=JsonDocument.Parse(File.ReadAllText(journal));
        const string operationId="366bd44d-a0f6-ba07-b9f4-b79f5f1d2bbe";
        Assert.Equal(operationId,intent.RootElement.GetProperty("operationId").GetString());
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.Equal("http://127.0.0.1:26657",Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_RPC"));
        Assert.Equal("oasis-cosmos-local",Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_CHAIN"));
        Assert.Equal(intent.RootElement.GetProperty("contract").GetString(),Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_CONTRACT"));
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(90));var token=deadline.Token;
        var store=new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing");
        var operation=await store.GetAsync(operationId,token);Assert.NotNull(operation);
        var before=JsonSerializer.Serialize(operation);Assert.Null(operation.Settlement);
        using var cosmos=new CosmosBlockChainOASIS(rpcEndpoint:"http://127.0.0.1:26657",chainId:"oasis-cosmos-local",
            contractAddress:intent.RootElement.GetProperty("contract").GetString()!,addressPrefix:"wasm",gasPrice:"0.025stake",nativeDenom:"stake",nativeDecimals:0,
            storageEncryptionKeyResolver:()=>SHA256.HashData(Encoding.UTF8.GetBytes("oasis-cosmos-local-storage-fixture-v1")));
        var originals=operation.ProviderAttempts.Where(attempt=>attempt.Provider=="CosmosBlockChainOASIS").ToArray();
        Assert.InRange(originals.Length,5,6);
        var receipts=new List<object>();
        foreach(var original in originals){
            var receipt=await cosmos.GetPreparedNativeFeeReceiptAsync(original,token);
            receipts.Add(new{original,feeAvailable=!receipt.IsError,error=receipt.Message,receipt=receipt.IsError?(JsonElement?)null:receipt.Result});
        }
        var backend=(IKeyValueBackend)typeof(CosmosBlockChainOASIS).GetField("_privateStorage",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(cosmos)!;
        var avatarId=intent.RootElement.GetProperty("avatarId").GetGuid();var leafId=intent.RootElement.GetProperty("holonId").GetGuid();
        var states=new List<object>();
        foreach(var key in new[]{"oasis/avatar/"+avatarId.ToString("N"),"oasis/avatardetail/"+avatarId.ToString("N"),"oasis/holon/"+leafId.ToString("N")}){
            var record=await backend.GetAsync(key,token);
            states.Add(new{key,present=record!=null});
        }
        var local=new LocalFileOASIS(storageDirectory:Path.Combine(root,"files"));
        bool localAvatarPresent,localDetailPresent,localLeafPresent;
        try{
            localAvatarPresent=(await local.LoadAvatarAsync(avatarId)).Result!=null;
            localDetailPresent=(await local.LoadAvatarDetailAsync(avatarId)).Result!=null;
            localLeafPresent=(await local.LoadHolonAsync(leafId,loadChildren:false)).Result!=null;
        }finally{await local.DeActivateProviderAsync();}
        Assert.Equal(before,JsonSerializer.Serialize(await store.GetAsync(operationId,token)));
        Assert.Equal(hash,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(journal))));
        Assert.False(cosmos.IsProviderActivated);
        var report=JsonSerializer.Serialize(new{operationId,originalJournalSha256=hash,originals=receipts,nativeStates=states,
            localAvatarPresent,localDetailPresent,localLeafPresent,readOnly=true,ledgerUnchanged=true,replacementAuthorized=false,logicalOutcomeResolved=false});
        await File.WriteAllTextAsync(Path.Combine(root,"routing-outage-original-inspection-current.json"),report,token);output.WriteLine(report);
    }

    [Fact]
    public async Task ReconcileExactFailedProfileWithNewAdmittedCleanupWithoutReplay()
    {
        // Revalidate original signatures, native state and exact local failure before authorizing any cleanup.
        await InspectCurrentFailedProfileOriginalsWithoutMutation();
        var root = Environment.GetEnvironmentVariable("OASIS_HYPERDRIVE_TEST_ROOT")!;
        string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        var journal = Path.Combine(root, "independent-routing-pending.json");
        var outageIncident=Hash(journal)=="CE9E54E7E52BC2F8C734A37E4FB84E081D3AC686739C8FE8798EF37C26CE6A48";
        if(!outageIncident)Assert.Equal("F901B2959830F84882BBC338992EF734612D7009BBF318E36F178E279CE98BC1",Hash(journal));
        var archivePrefix=outageIncident?"routing-outage":"routing-profile";
        var cleanupJournal = Path.Combine(root, "routing-cleanup-pending.json");
        var archive = Path.Combine(root, archivePrefix+"-reviewed-reconciliation.json");
        var originalArchive = Path.Combine(root, archivePrefix+"-original-intent-resolved.json");
        var cleanupArchive = Path.Combine(root, archivePrefix+"-cleanup-intent-resolved.json");
        foreach (var path in new[] { cleanupJournal, archive, originalArchive, cleanupArchive }) Assert.False(File.Exists(path));
        using var intent = JsonDocument.Parse(File.ReadAllText(journal));
        var avatarId = intent.RootElement.GetProperty("avatarId").GetGuid();
        var leafId = intent.RootElement.GetProperty("holonId").GetGuid();
        var originalId = outageIncident?"366bd44d-a0f6-ba07-b9f4-b79f5f1d2bbe":"537d7453-bfa6-12e7-8cb8-e3af7c758df5";
        const string connection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(150)); var token = deadline.Token;
        var store = new MongoUsageOutboxStore(connection, "usage_cosmos_independent_routing");
        var original = await store.GetAsync(originalId, token); var originalSnapshot = JsonSerializer.Serialize(original);
        var sender = original.ProviderAttempts.First(attempt => attempt.Provider == "CosmosBlockChainOASIS").NativePreparation.Sender;
        using var cosmos = CosmosBlockChainOASIS.CreateWithNativeFeeLimits(
            CosmosNativeFeeLimits.ForReviewedStandardSdkChain(new Dictionary<string, string> { ["stake"] = "50000" }, "oasis-cosmos-local"),
            rpcEndpoint: "http://127.0.0.1:26657", chainId: "oasis-cosmos-local",
            privateKey: Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_KEY")!,
            contractAddress: intent.RootElement.GetProperty("contract").GetString()!,
            addressPrefix: "wasm", gasPrice: "0.025stake", nativeDenom: "stake", nativeDecimals: 0,
            storageEncryptionKeyResolver: () => SHA256.HashData(Encoding.UTF8.GetBytes("oasis-cosmos-local-storage-fixture-v1")));
        var originalAttempts=original.ProviderAttempts.Where(attempt=>attempt.Provider=="CosmosBlockChainOASIS").ToArray();
        Assert.Equal(5,originalAttempts.Length);
        foreach(var attempt in originalAttempts){
            var confirmed=await cosmos.GetPreparedNativeFeeReceiptAsync(attempt,token);
            Assert.False(confirmed.IsError,confirmed.Message);Assert.True(confirmed.Result.GetProperty("success").GetBoolean());
        }
        var user = Guid.NewGuid().ToString("D");
        using var http = new HttpClient(new CosmosIndependentRoutingTests.TestAuthority(user));
        var authority = new Web4SubscriptionAuthorizationClient(http, "https://offline-authority.invalid", "WEB6", "test-only-key");
        var execution = await new Web4UsageExecution(authority, store, new Web4UsageLedgerOptions { Service = "WEB6" }).BeginAsync(
            "test-only", user, "reviewed-profile-cleanup-" + originalId, new string('c', 64), "POST /reviewed-profile-cleanup",
            new UsageEndpointPolicy { MeterCategory = "api.request", ReservedUnits = 1, EstimatedCostUsd = .25m, MaximumNativeFeeCostUsd = .25m }, token);
        var now = DateTime.UtcNow.AddSeconds(-1);
        execution = execution.WithNativePrices(new[] { new NativeFeePriceQuote(Guid.Parse(execution.OperationId), "CosmosBlockChainOASIS", "oasis-cosmos-local",
            "stake", "local-test-stake", 0, .000001m, "synthetic-local-price", "fixture-v1", "fixture-v1", now, now, now, now.AddMinutes(5)) });
        using var file = new FileStream(cleanupJournal, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(file, new { originalId, avatarId, leafId, cleanupOperationId = execution.OperationId,
            originalJournalSha256 = Hash(journal), database = "usage_cosmos_independent_routing", recovery = "Inspect original cleanup attempts; never replay uncertain deletes." }, cancellationToken: token);
        file.Flush(true);
        await cosmos.ExecutePreparedStorageAsync(async () => {
            var profile = await cosmos.LoadAvatarAsync(avatarId);
            Assert.False(profile.IsError, profile.Message);
            Assert.Equal(intent.RootElement.GetProperty("Username").GetString(), profile.Result.Username);
            var leaf = await cosmos.LoadHolonAsync(leafId, loadChildren: false);
            Assert.False(leaf.IsError, leaf.Message); Assert.Equal("independent-cosmos-v2-leaf", leaf.Result.Name);
            var deleted = await cosmos.DeleteHolonAsync(leafId);
            Assert.False(deleted.IsError, deleted.Message);
            var deletedProfile = await cosmos.DeleteAvatarAsync(avatarId, softDelete: false);
            Assert.False(deletedProfile.IsError, deletedProfile.Message); Assert.True(deletedProfile.Result);
            return true;
        }, execution, "reviewed-profile-cleanup", sender, token);
        var cleanup = await store.GetAsync(execution.OperationId, token);
        Assert.Equal(5, cleanup.ProviderAttempts.Count);
        var fees = new List<object>();
        foreach (var attempt in cleanup.ProviderAttempts)
        {
            var receipt = await cosmos.GetPreparedNativeFeeReceiptAsync(attempt, token);
            Assert.False(receipt.IsError, receipt.Message); Assert.True(receipt.Result.GetProperty("success").GetBoolean());
            fees.Add(new { attempt, anteFeeReceipt = receipt.Result });
        }
        Assert.Null((await cosmos.LoadAvatarAsync(avatarId)).Result);
        Assert.Null((await cosmos.LoadAvatarDetailAsync(avatarId)).Result);
        Assert.Null((await cosmos.LoadHolonAsync(leafId, loadChildren: false)).Result);
        Assert.Null((await cosmos.LoadAvatarByUsernameAsync(intent.RootElement.GetProperty("Username").GetString()!)).Result);
        Assert.Null((await cosmos.LoadAvatarByEmailAsync(intent.RootElement.GetProperty("Email").GetString()!)).Result);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        try
        {
            if(outageIncident)Assert.Null((await local.LoadHolonAsync(leafId,loadChildren:false)).Result);
            else {var deleted = await local.DeleteHolonAsync(leafId); Assert.False(deleted.IsError, deleted.Message);}
            var profileDeleted = await local.DeleteAvatarAsync(avatarId, softDelete: false); Assert.False(profileDeleted.IsError, profileDeleted.Message);
            Assert.Null((await local.LoadAvatarAsync(avatarId)).Result);
            Assert.Null((await local.LoadAvatarDetailAsync(avatarId)).Result);
            Assert.Null((await local.LoadHolonAsync(leafId, loadChildren: false)).Result);
        }
        finally { await local.DeActivateProviderAsync(); }
        Assert.Equal(originalSnapshot, JsonSerializer.Serialize(await store.GetAsync(originalId, token)));
        Assert.Equal("9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81", Hash(Path.Combine(root, "replication-pending.json")));
        var evidence = JsonSerializer.Serialize(new { originalIntent = intent.RootElement, original,
            cleanup, cleanupOriginals = fees, exactFixtureCleanupVerified = true, originalLedgerRetained = true,
            originalLogicalTestSucceeded = false, finalFeeCoverageVerified = false, settlementAuthorized = false });
        using (var archiveFile = new FileStream(archive, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        { await archiveFile.WriteAsync(Encoding.UTF8.GetBytes(evidence), token); archiveFile.Flush(true); }
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(evidence))), Hash(archive));
        file.Dispose();
        File.Move(journal, originalArchive); File.Move(cleanupJournal, cleanupArchive);
        output.WriteLine("Exact synthetic fixture cleaned using five NEW admitted deletes; original ledger retained; archive SHA256=" + Hash(archive));
    }

    [Fact]
    public async Task InspectExactFailedProfileOriginalsWithoutMutation()
    {
        string Required(string key) => Environment.GetEnvironmentVariable(key) ?? throw new InvalidOperationException(key);
        string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        var root = Required("OASIS_HYPERDRIVE_TEST_ROOT");
        var journal = Path.Combine(root, "independent-routing-pending.json");
        const string journalHash = "F901B2959830F84882BBC338992EF734612D7009BBF318E36F178E279CE98BC1";
        const string oldHash = "9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81";
        Assert.Equal(journalHash, Hash(journal));
        Assert.Equal(oldHash, Hash(Path.Combine(root, "replication-pending.json")));
        using var intent = JsonDocument.Parse(File.ReadAllText(journal));
        Assert.Equal("537d7453-bfa6-12e7-8cb8-e3af7c758df5", intent.RootElement.GetProperty("operationId").GetString());
        Assert.Equal("usage_cosmos_independent_routing", intent.RootElement.GetProperty("database").GetString());
        Assert.Equal("http://127.0.0.1:26657", Required("OASIS_COSMOS_TEST_RPC"));
        Assert.Equal("oasis-cosmos-local", Required("OASIS_COSMOS_TEST_CHAIN"));
        Assert.Equal(Required("OASIS_COSMOS_TEST_CONTRACT"), intent.RootElement.GetProperty("contract").GetString());
        const string connection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection, Required("SUBSCRIPTION_TEST_MONGODB_URI"));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var token = deadline.Token;
        var store = new MongoUsageOutboxStore(connection, "usage_cosmos_independent_routing");
        var operation = await store.GetAsync("537d7453-bfa6-12e7-8cb8-e3af7c758df5", token);
        Assert.NotNull(operation);
        var before = JsonSerializer.Serialize(operation);
        Assert.Null(operation.Settlement);
        // No signing key or provider activation: this path only verifies persisted signatures and reads indexed originals.
        using var cosmos = new CosmosBlockChainOASIS(rpcEndpoint: Required("OASIS_COSMOS_TEST_RPC"),
            chainId: "oasis-cosmos-local", contractAddress: Required("OASIS_COSMOS_TEST_CONTRACT"),
            addressPrefix: "wasm", gasPrice: "0.025stake", nativeDenom: "stake", nativeDecimals: 0,
            storageEncryptionKeyResolver: () => SHA256.HashData(Encoding.UTF8.GetBytes("oasis-cosmos-local-storage-fixture-v1")));
        var originals = operation.ProviderAttempts.Where(attempt => attempt.Provider == "CosmosBlockChainOASIS").ToArray();
        Assert.Equal(5, originals.Length);
        Assert.Equal(5, originals.Select(attempt => attempt.ProviderRequestId).Distinct().Count());
        var receipts = new List<object>();
        foreach (var original in originals)
        {
            var receipt = await cosmos.GetPreparedNativeFeeReceiptAsync(original, token);
            Assert.False(receipt.IsError, receipt.Message);
            Assert.Equal(original.ProviderRequestId, receipt.Result.GetProperty("transactionHash").GetString());
            Assert.True(receipt.Result.GetProperty("success").GetBoolean());
            receipts.Add(new { original, anteFeeReceipt = receipt.Result });
        }
        // Read exact known records directly; public provider activation intentionally requires a signing key.
        var storage = (IKeyValueBackend)typeof(CosmosBlockChainOASIS).GetField("_privateStorage", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cosmos)!;
        var avatarId = intent.RootElement.GetProperty("avatarId").GetGuid();
        var leafId = intent.RootElement.GetProperty("holonId").GetGuid();
        foreach (var key in new[] { "oasis/avatar/" + avatarId.ToString("N"), "oasis/avatardetail/" + avatarId.ToString("N"), "oasis/holon/" + leafId.ToString("N") })
        {
            var record = await storage.GetAsync(key, token);
            Assert.NotNull(record);
            using var parsed = JsonDocument.Parse(record);
            Assert.Equal(key.EndsWith(leafId.ToString("N")) ? leafId : avatarId, parsed.RootElement.GetProperty("Id").GetGuid());
        }
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        try
        {
            var byId = await local.LoadAvatarAsync(avatarId);
            Assert.False(byId.IsError, byId.Message);
            Assert.Equal(avatarId, byId.Result?.Id);
            Assert.Empty(byId.Result!.ProviderUniqueStorageKey);
            var byKey = await local.LoadAvatarByProviderKeyAsync(avatarId.ToString());
            Assert.False(byKey.IsError, byKey.Message);
            Assert.Null(byKey.Result); // Exact retained failure; do not rewrite the incident's evidence.
        }
        finally { await local.DeActivateProviderAsync(); }
        Assert.Equal(before, JsonSerializer.Serialize(await store.GetAsync("537d7453-bfa6-12e7-8cb8-e3af7c758df5", token)));
        Assert.Equal(journalHash, Hash(journal));
        Assert.Equal(oldHash, Hash(Path.Combine(root, "replication-pending.json")));
        Assert.False(cosmos.IsProviderActivated);
        var report = JsonSerializer.Serialize(new { originalJournalSha256 = journalHash, operationId = "537d7453-bfa6-12e7-8cb8-e3af7c758df5",
            originals = receipts, readOnly = true, ledgerUnchanged = true, replacementAuthorized = false,
            threeNativeRecordsVerified = true, retainedLocalProviderKeyGapVerified = true,
            finalFeeCoverageVerified = false, logicalOutcomeResolved = false });
        await File.WriteAllTextAsync(Path.Combine(root, "routing-profile-original-inspection-current.json"), report, token);
        output.WriteLine(report);
    }
}
