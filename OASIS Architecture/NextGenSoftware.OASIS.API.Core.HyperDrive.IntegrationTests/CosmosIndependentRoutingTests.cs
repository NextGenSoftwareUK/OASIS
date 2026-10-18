using System.Reflection;
using System.Text.Json;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmosIndependentRoutingTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task IndependentLeafReplicatesThroughRealCosmosWithPreparedOriginals(bool nativePrimary,bool synchronous)
    {
        string Required(string name)=>Environment.GetEnvironmentVariable(name)??throw new InvalidOperationException(name+" required.");
        var root=Required("OASIS_HYPERDRIVE_TEST_ROOT");
        string Hash(string path)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
        const string oldHash="9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81";
        var oldPath=Path.Combine(root,"replication-pending.json");Assert.Equal(oldHash,Hash(oldPath));
        using var old=JsonDocument.Parse(File.ReadAllText(oldPath));
        var id=Guid.NewGuid();Assert.NotEqual(old.RootElement.GetProperty("holonId").GetGuid(),id);
        var avatarId=Guid.NewGuid();Assert.NotEqual(old.RootElement.GetProperty("avatarId").GetGuid(),avatarId);
        var avatar=new Avatar{Id=avatarId,Username="cosmos-independent-"+avatarId.ToString("N"),Email=avatarId.ToString("N")+"@example.invalid"};
        var detail=new AvatarDetail{Id=avatarId,Username=avatar.Username,Email=avatar.Email};
        var journal=Path.Combine(root,"independent-routing-pending.json");Assert.False(File.Exists(journal));
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection,Required("SUBSCRIPTION_TEST_MONGODB_URI"));
        const string database="usage_cosmos_independent_routing";
        var mongo=new MongoClient(connection);using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(180));var token=deadline.Token;
        using(var names=await mongo.ListDatabaseNamesAsync(cancellationToken:token))Assert.DoesNotContain(database,await names.ToListAsync(token));
        var rpc=Required("OASIS_COSMOS_TEST_RPC");Assert.Equal("http://127.0.0.1:26657",rpc);
        var chain=Required("OASIS_COSMOS_TEST_CHAIN");Assert.Equal("oasis-cosmos-local",chain);
        const string keyLabel="oasis-cosmos-local-storage-fixture-v1";
        using var cosmos=CosmosBlockChainOASIS.CreateWithNativeFeeLimits(CosmosNativeFeeLimits.ForReviewedStandardSdkChain(new Dictionary<string,string>{{"stake","50000"}},chain),
            rpcEndpoint:rpc,chainId:chain,privateKey:Required("OASIS_COSMOS_TEST_KEY"),contractAddress:Required("OASIS_COSMOS_TEST_CONTRACT"),
            addressPrefix:"wasm",gasPrice:"0.025stake",nativeDenom:"stake",nativeDecimals:0,
            storageEncryptionKeyResolver:()=>System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(keyLabel)));
        var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(cosmos)!;
        var invoke=backend.GetType().GetMethod("InvokeAsync",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var identity=await (Task<JsonElement>)invoke.Invoke(backend,new object[]{"restoreKey",new{},token})!;
        var sender=identity.GetProperty("address").GetString()!;
        var local=new LocalFileOASIS(storageDirectory:Path.Combine(root,"files"));
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
        dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(cosmos);manager.RegisterProvider(local);
        var configured=manager.SetAndReplaceAutoReplicationListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new EnumValue<ProviderType>(ProviderType.CosmosBlockChainOASIS)});
        Assert.False(configured.IsError,configured.Message);var router=new OASISHyperDrive(manager);
        var preferred=nativePrimary?ProviderType.CosmosBlockChainOASIS:ProviderType.LocalFileOASIS;
        using var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);var complete=false;
        try{
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new TestAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.invalid","WEB6","test-only-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"}).BeginAsync("test-only",user,id.ToString("D"),new string('a',64),
                "POST /independent-native-routing",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=0.75m,MaximumNativeFeeCostUsd=0.75m,MaximumProviderAttempts=128},token);
            var time=DateTime.UtcNow.AddSeconds(-1);
            execution=execution.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(execution.OperationId),"CosmosBlockChainOASIS",chain,"stake","local-test-stake",0,0.000001m,
                "synthetic-local-price","fixture-v1","fixture-v1",time,time,time,time.AddMinutes(5))});
            await JsonSerializer.SerializeAsync(file,new{holonId=id,avatarId,avatar.Username,avatar.Email,chain,contract=Required("OASIS_COSMOS_TEST_CONTRACT"),nativePrimary,synchronous,
                encryptionKeyLabel=keyLabel,database,operationId=execution.OperationId,preservedOriginalJournalSha256=oldHash,
                recovery="Inspect exact new profile/detail/leaf and original attempts; never replay unknown routing or alter replication-pending.json."},cancellationToken:token);file.Flush(true);
            var policy=new PreparedStorageExecution(execution,"independent",new Dictionary<ProviderType,string>{{ProviderType.CosmosBlockChainOASIS,sender}},new[]{ProviderType.LocalFileOASIS});
            var nonNativeReads=0;
            await policy.RunAsync(async()=>{
                var profileWrite=new StorageOperationRequest{Operation="SaveAvatar",Payload=avatar,PreferredProvider=preferred};
                var savedProfile=synchronous?router.RouteRequest<IAvatar>(profileWrite):await router.RouteRequestAsync<IAvatar>(profileWrite);
                Assert.False(savedProfile.IsError,savedProfile.Message);Assert.False(savedProfile.IsWarning,savedProfile.Message);
                Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                var detailWrite=new StorageOperationRequest{Operation="SaveAvatarDetail",Payload=detail,PreferredProvider=preferred};
                var savedDetail=synchronous?router.RouteRequest<IAvatarDetail>(detailWrite):await router.RouteRequestAsync<IAvatarDetail>(detailWrite);
                Assert.False(savedDetail.IsError,savedDetail.Message);Assert.False(savedDetail.IsWarning,savedDetail.Message);
                Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                var holon=new Holon{Id=id,Name="independent-cosmos-v2-leaf",MetaData=new Dictionary<string,object>{{"owned-routing-fixture",id.ToString()}}};
                var write=new StorageOperationRequest{Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=preferred};
                var saved=synchronous?router.RouteRequest<IHolon>(write):await router.RouteRequestAsync<IHolon>(write);
                Assert.False(saved.IsError,saved.Message);Assert.False(saved.IsWarning,saved.Message);
                Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                Assert.Equal(holon.Name,(await cosmos.LoadHolonAsync(id,loadChildren:false)).Result?.Name);
                Assert.Equal(holon.Name,(await local.LoadHolonAsync(id,loadChildren:false)).Result?.Name);
                var providers=new[]{ProviderType.LocalFileOASIS,ProviderType.CosmosBlockChainOASIS};
                async Task ReadProfiles(bool absent){
                    foreach(var providerType in providers){
                        var before=manager.PerformanceMonitor.GetMetrics(providerType).TotalRequests;
                        foreach(var suffix in new[]{"","ByProviderKey","ByUsername","ByEmail"}){
                            var request=new StorageOperationRequest{Operation="LoadAvatar"+suffix,AvatarId=avatarId,ProviderKey=avatarId.ToString(),Username=avatar.Username,Email=avatar.Email,PreferredProvider=providerType};
                            foreach(var read in new[]{router.RouteRequest<IAvatar>(request),await router.RouteRequestAsync<IAvatar>(request)}){
                                Assert.True(!read.IsError||absent&&read.ErrorCode==NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,read.Message);
                                if(absent)Assert.Null(read.Result);else {Assert.Equal(avatarId,read.Result?.Id);Assert.Equal(avatar.Username,read.Result?.Username);Assert.Equal(avatar.Email,read.Result?.Email);}
                            }
                            // The storage contract has detail selectors for ID/username/email, not provider key.
                            if(suffix=="ByProviderKey")continue;
                            request.Operation="LoadAvatarDetail"+suffix;
                            foreach(var read in new[]{router.RouteRequest<IAvatarDetail>(request),await router.RouteRequestAsync<IAvatarDetail>(request)}){
                                Assert.True(!read.IsError||absent&&read.ErrorCode==NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,read.Message);
                                if(absent)Assert.Null(read.Result);else {Assert.Equal(avatarId,read.Result?.Id);Assert.Equal(avatar.Username,read.Result?.Username);Assert.Equal(avatar.Email,read.Result?.Email);}
                            }
                        }
                        Assert.Equal(before+14,manager.PerformanceMonitor.GetMetrics(providerType).TotalRequests);
                        if(providerType==ProviderType.LocalFileOASIS)nonNativeReads+=14;
                    }
                }
                await ReadProfiles(false);
                foreach(var providerType in providers){
                    var before=manager.PerformanceMonitor.GetMetrics(providerType).TotalRequests;
                    foreach(var operationName in new[]{"LoadHolon","LoadHolonByProviderKey"}){
                        var request=new StorageOperationRequest{Operation=operationName,HolonId=id,ProviderKey=id.ToString(),PreferredProvider=providerType,LoadChildren=false};
                        foreach(var read in new[]{router.RouteRequest<IHolon>(request),await router.RouteRequestAsync<IHolon>(request)}){
                            Assert.False(read.IsError,read.Message);Assert.Equal(id,read.Result?.Id);Assert.Equal(holon.Name,read.Result?.Name);
                        }
                    }
                    foreach(var operationName in new[]{"LoadHolonsByMetaData","LoadHolonsByMetaDataPairs"}){
                        var request=new StorageOperationRequest{Operation=operationName,PreferredProvider=providerType,LoadChildren=false,
                            MetaKey="owned-routing-fixture",MetaValue=id.ToString(),MetaKeyValuePairs=new Dictionary<string,string>{{"owned-routing-fixture",id.ToString()}}};
                        foreach(var read in new[]{router.RouteRequest<IEnumerable<IHolon>>(request),await router.RouteRequestAsync<IEnumerable<IHolon>>(request)}){
                            Assert.False(read.IsError,read.Message);Assert.Equal(holon.Name,Assert.Single(read.Result).Name);
                            Assert.Equal(id,Assert.Single(read.Result).Id);
                        }
                    }
                    Assert.Equal(before+8,manager.PerformanceMonitor.GetMetrics(providerType).TotalRequests);
                    if(providerType==ProviderType.LocalFileOASIS)nonNativeReads+=8;
                }
                Assert.False(manager.SetAndReplaceAutoLoadBalanceListForProviders(providers.Select(type=>new EnumValue<ProviderType>(type))).IsError);
                manager.IsAutoReplicationEnabled=false;manager.IsAutoLoadBalanceEnabled=true;
                async Task<ProviderType> BalancedRead(LoadBalancingStrategy strategy,bool sync){
                    var counts=providers.ToDictionary(type=>type,type=>manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
                    var request=new StorageOperationRequest{Operation="LoadHolon",HolonId=id,LoadChildren=false};
                    var read=sync?router.RouteRequest<IHolon>(request,strategy):await router.RouteRequestAsync<IHolon>(request,strategy);
                    Assert.False(read.IsError,read.Message);Assert.Equal(holon.Name,read.Result?.Name);
                    var selected=manager.LastProviderSelectionDiagnostic;Assert.Equal(strategy,selected.RequestedStrategy);
                    Assert.Contains(selected.SelectedProvider,providers);
                    foreach(var type in providers)Assert.Equal(counts[type]+(type==selected.SelectedProvider?1:0),manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
                    if(selected.SelectedProvider==ProviderType.LocalFileOASIS)nonNativeReads++;
                    return selected.SelectedProvider;
                }
                try{
                    foreach(var strategy in Enum.GetValues<LoadBalancingStrategy>())foreach(var sync in new[]{false,true}){
                        await BalancedRead(strategy,sync);
                        Assert.Equal(providers.OrderBy(type=>type),manager.LastProviderSelectionDiagnostic.Candidates.Select(candidate=>candidate.Provider).OrderBy(type=>type));
                    }
                    var roundRobin=new Dictionary<ProviderType,int>{{providers[0],0},{providers[1],0}};
                    for(var index=0;index<6;index++)roundRobin[await BalancedRead(LoadBalancingStrategy.RoundRobin,false)]++;
                    Assert.All(roundRobin.Values,count=>Assert.Equal(3,count));
                    dna.OASIS.SubscriptionConfig.PlanType="Free";
                    var excluded=manager.PerformanceMonitor.GetMetrics(ProviderType.CosmosBlockChainOASIS).TotalRequests;
                    foreach(var strategy in Enum.GetValues<LoadBalancingStrategy>()){
                        Assert.Equal(ProviderType.LocalFileOASIS,await BalancedRead(strategy,false));
                        Assert.Equal(ProviderType.LocalFileOASIS,Assert.Single(manager.LastProviderSelectionDiagnostic.Candidates).Provider);
                    }
                    Assert.Equal(excluded,manager.PerformanceMonitor.GetMetrics(ProviderType.CosmosBlockChainOASIS).TotalRequests);
                    Assert.Equal(5,(await store.GetAsync(execution.OperationId,token)).ProviderAttempts.Count(attempt=>attempt.Provider=="CosmosBlockChainOASIS"));
                }finally{
                    dna.OASIS.SubscriptionConfig.PlanType="Gold";manager.IsAutoLoadBalanceEnabled=false;manager.IsAutoReplicationEnabled=true;
                }
                var remove=new StorageOperationRequest{Operation="DeleteHolon",HolonId=id,SoftDelete=false,PreferredProvider=preferred,LoadChildren=false};
                var deleted=synchronous?router.RouteRequest<IHolon>(remove):await router.RouteRequestAsync<IHolon>(remove);
                Assert.False(deleted.IsError,deleted.Message);Assert.False(deleted.IsWarning,deleted.Message);
                Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                Assert.Null((await cosmos.LoadHolonAsync(id,loadChildren:false)).Result);
                Assert.Null((await local.LoadHolonAsync(id,loadChildren:false)).Result);
                var removeProfile=new StorageOperationRequest{Operation="DeleteAvatar",AvatarId=avatarId,SoftDelete=false,PreferredProvider=preferred};
                var deletedProfile=synchronous?router.RouteRequest<bool>(removeProfile):await router.RouteRequestAsync<bool>(removeProfile);
                Assert.False(deletedProfile.IsError,deletedProfile.Message);Assert.False(deletedProfile.IsWarning,deletedProfile.Message);Assert.True(deletedProfile.Result);
                Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                await ReadProfiles(true);return true;
            });
            var operation=await new MongoUsageOutboxStore(connection,database).GetAsync(execution.OperationId,token);
            var native=operation.ProviderAttempts.Where(attempt=>attempt.Provider=="CosmosBlockChainOASIS").ToArray();
            Assert.Equal(10,native.Length);Assert.Equal(5+nonNativeReads,operation.ProviderAttempts.Count(attempt=>attempt.Provider=="LocalFileOASIS"));
            Assert.Equal(native.Sum(attempt=>NativeFeeMaximumAccounting.ValueUsd(attempt.NativePreparation.FeeMaximums,attempt.NativePreparation.PriceQuotes)),operation.NativeFeeExposureUsd);
            var originals=new List<object>();
            foreach(var attempt in native){
                Assert.Null(attempt.AcknowledgedAtUtc);var fee=await cosmos.GetPreparedNativeFeeReceiptAsync(attempt,token);Assert.False(fee.IsError,fee.Message);
                Assert.Equal(attempt.ProviderRequestId,fee.Result.GetProperty("transactionHash").GetString());
                originals.Add(new{attempt,anteFeeReceipt=fee.Result});
            }
            output.WriteLine(JsonSerializer.Serialize(new{nativePrimary,synchronous,holonId=id,avatarId,operation.NativeFeeExposureUsd,originals,
                profileMatchingReadCount=28,profileAbsenceReadCount=28,selectorReadCount=16,strategyReadCount=2*Enum.GetValues<LoadBalancingStrategy>().Length,roundRobinReadCount=6,freeReadCount=Enum.GetValues<LoadBalancingStrategy>().Length,nonNativeReads,hostedSettlementVerified=false}));
            Assert.Equal(oldHash,Hash(oldPath));Assert.Null(operation.Settlement);complete=true;
        }finally{
            if(complete){await mongo.DropDatabaseAsync(database,token);file.Dispose();File.Delete(journal);}
            else Console.WriteLine("Preserved independent routing journal/outbox: "+journal);
            await local.DeActivateProviderAsync();
        }
    }

    private sealed class TestAuthority(string user):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
            var body=await request.Content!.ReadAsStringAsync(token);object result;
            if(request.RequestUri!.AbsolutePath.EndsWith("authorize")){var input=JsonSerializer.Deserialize<UsageAuthorizationRequest>(body)!;
                result=new SubscriptionAuthorizationResult{Allowed=true,StatusCode=200,UserId=user,OperationId=input.OperationId,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(3)};
            }else if(request.RequestUri.AbsolutePath.EndsWith("start")){var input=JsonSerializer.Deserialize<UsageStartRequest>(body)!;result=new UsageStartResult{Started=true,OperationId=input.OperationId,Status="executing"};}
            else throw new InvalidOperationException("Unexpected test-authority request.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(result))};
        }
    }
}
