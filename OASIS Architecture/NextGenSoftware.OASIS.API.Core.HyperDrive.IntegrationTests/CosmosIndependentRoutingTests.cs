using System.Reflection;
using System.Numerics;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
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
    public Task IndependentLeafReplicatesThroughRealCosmosWithPreparedOriginals(bool nativePrimary,bool synchronous)
        => RunRoutingAsync(nativePrimary,synchronous,false);

    [Theory][InlineData(false)][InlineData(true)]
    public Task VersionedRecordsReplicateAndRetainHistoricalValues(bool synchronous)
        => RunRoutingAsync(true,synchronous,true);

    private async Task RunRoutingAsync(bool nativePrimary,bool synchronous,bool versioned)
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
        avatar.MetaData=new(){{"Latitude",0d},{"Longitude",179.999999d}};
        if(versioned)avatar.MetaData["version-proof"]="original";
        var detail=new AvatarDetail{Id=avatarId,Username=avatar.Username,Email=avatar.Email};
        var journal=Path.Combine(root,"independent-routing-pending.json");Assert.False(File.Exists(journal));
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection,Required("SUBSCRIPTION_TEST_MONGODB_URI"));
        const string database="usage_cosmos_independent_routing";
        var mongo=new MongoClient(connection);using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(480));var token=deadline.Token;
        // Keep compact original-attempt evidence across cases; never discard a previous incident's ledger.
        var rpc=Required("OASIS_COSMOS_TEST_RPC");Assert.Equal("http://127.0.0.1:26657",rpc);
        var chain=Required("OASIS_COSMOS_TEST_CHAIN");Assert.Equal("oasis-cosmos-local",chain);
        const string keyLabel="oasis-cosmos-local-storage-fixture-v1";
        var deployment=await CosmosReviewedFinalFeeTests.CreateReviewedProviderAsync(id,token);
        using var cosmos=deployment.Provider;var review=deployment.Review;
        cosmos.IsVersionControlEnabled=versioned;
        var feeReader=new RegisteredNativeFeeReader(ProviderType.CosmosBlockChainOASIS,chain,new IOASISProvider[]{cosmos});
        await new StorageAdmissionRecoveryReadiness(new[]{("CosmosBlockChainOASIS",chain)},new[]{feeReader}).StartAsync(token);
        var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(cosmos)!;
        var invoke=backend.GetType().GetMethod("InvokeAsync",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var identity=await (Task<JsonElement>)invoke.Invoke(backend,new object[]{"restoreKey",new{},token})!;
        var sender=identity.GetProperty("address").GetString()!;
        async Task<BigInteger> Balance(CancellationToken balanceToken)=>BigInteger.Parse((await (Task<JsonElement>)invoke.Invoke(backend,new object[]{"balance",new{walletAddress=sender,denom="stake"},balanceToken})!).GetProperty("amount").GetString()!,System.Globalization.CultureInfo.InvariantCulture);
        var walletBefore=await Balance(token);
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
            // 21 originals * 50000stake maximum * synthetic .000001USD =1.05USD.
            // This NEW isolated scenario reserves1.25; original journals/budgets are untouched.
            var maximumNativeUsd=versioned?1.25m:0.75m;
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"}).BeginAsync("test-only",user,id.ToString("D"),new string('a',64),
                "POST /independent-native-routing",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=maximumNativeUsd,MaximumNativeFeeCostUsd=maximumNativeUsd,MaximumProviderAttempts=128},token);
            var time=DateTime.UtcNow.AddSeconds(-1);
            execution=execution.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(execution.OperationId),"CosmosBlockChainOASIS",chain,"stake","local-test-stake",0,0.000001m,
                "synthetic-local-price","fixture-v1","fixture-v1",time,time,time,time.AddMinutes(10))});
            await JsonSerializer.SerializeAsync(file,new{holonId=id,avatarId,avatar.Username,avatar.Email,chain,contract=Required("OASIS_COSMOS_TEST_CONTRACT"),nativePrimary,synchronous,
                encryptionKeyLabel=keyLabel,database,operationId=execution.OperationId,review,versioned,maximumNativeUsd,walletBefore=walletBefore.ToString(System.Globalization.CultureInfo.InvariantCulture),preservedOriginalJournalSha256=oldHash,
                recovery="Inspect exact new profile/detail/leaf and original attempts; never replay unknown routing or alter replication-pending.json."},cancellationToken:token);file.Flush(true);
            var policy=new PreparedStorageExecution(execution,"independent",new Dictionary<ProviderType,string>{{ProviderType.CosmosBlockChainOASIS,sender}},new[]{ProviderType.LocalFileOASIS});
            var nonNativeReads=0;var outageProfileReadCount=0;
            await policy.RunAsync(async()=>{
                var profileWrite=new StorageOperationRequest{Operation="SaveAvatar",Payload=avatar,PreferredProvider=preferred};
                var savedProfile=synchronous?router.RouteRequest<IAvatar>(profileWrite):await router.RouteRequestAsync<IAvatar>(profileWrite);
                Assert.False(savedProfile.IsError,savedProfile.Message);Assert.False(savedProfile.IsWarning,savedProfile.Message);
                Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                var detailWrite=new StorageOperationRequest{Operation="SaveAvatarDetail",Payload=detail,PreferredProvider=preferred};
                var savedDetail=synchronous?router.RouteRequest<IAvatarDetail>(detailWrite):await router.RouteRequestAsync<IAvatarDetail>(detailWrite);
                Assert.False(savedDetail.IsError,savedDetail.Message);Assert.False(savedDetail.IsWarning,savedDetail.Message);
                Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                var holon=new Holon{Id=id,Name="independent-cosmos-v2-leaf",MetaData=new Dictionary<string,object>{{"owned-routing-fixture",id.ToString()},{"Latitude",0d},{"Longitude",179.999999d}}};
                var write=new StorageOperationRequest{Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=preferred};
                var saved=synchronous?router.RouteRequest<IHolon>(write):await router.RouteRequestAsync<IHolon>(write);
                Assert.False(saved.IsError,saved.Message);Assert.False(saved.IsWarning,saved.Message);
                Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                Assert.Equal(holon.Name,(await cosmos.LoadHolonAsync(id,loadChildren:false)).Result?.Name);
                Assert.Equal(holon.Name,(await local.LoadHolonAsync(id,loadChildren:false)).Result?.Name);
                if(versioned){
                    var originalName=holon.Name;var originalXp=detail.XP;
                    avatar.MetaData["version-proof"]="updated";detail.XP=73;holon.Name+="-updated";
                    var avatarUpdate=new StorageOperationRequest{Operation="SaveAvatar",Payload=avatar,PreferredProvider=preferred};
                    var updatedAvatar=synchronous?router.RouteRequest<IAvatar>(avatarUpdate):await router.RouteRequestAsync<IAvatar>(avatarUpdate);
                    Assert.False(updatedAvatar.IsError,updatedAvatar.Message);Assert.False(updatedAvatar.IsWarning,updatedAvatar.Message);
                    Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                    var detailUpdate=new StorageOperationRequest{Operation="SaveAvatarDetail",Payload=detail,PreferredProvider=preferred};
                    var updatedDetail=synchronous?router.RouteRequest<IAvatarDetail>(detailUpdate):await router.RouteRequestAsync<IAvatarDetail>(detailUpdate);
                    Assert.False(updatedDetail.IsError,updatedDetail.Message);Assert.False(updatedDetail.IsWarning,updatedDetail.Message);
                    Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                    var holonUpdate=new StorageOperationRequest{Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=preferred};
                    var updatedHolon=synchronous?router.RouteRequest<IHolon>(holonUpdate):await router.RouteRequestAsync<IHolon>(holonUpdate);
                    Assert.False(updatedHolon.IsError,updatedHolon.Message);Assert.False(updatedHolon.IsWarning,updatedHolon.Message);
                    Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                    // LocalFile is a current-state replica, not a certified archival provider.
                    foreach(IOASISStorageProvider storage in new IOASISStorageProvider[]{cosmos}){
                        foreach(var historical in new[]{storage.LoadAvatar(avatarId,1),await storage.LoadAvatarAsync(avatarId,1)}){
                            Assert.False(historical.IsError,historical.Message);Assert.Equal(1,historical.Result.Version);
                            Assert.Equal("original",historical.Result.MetaData["version-proof"].ToString());
                        }
                        foreach(var historical in new[]{storage.LoadAvatarDetail(avatarId,1),await storage.LoadAvatarDetailAsync(avatarId,1)}){
                            Assert.False(historical.IsError,historical.Message);Assert.Equal(1,historical.Result.Version);Assert.Equal(originalXp,historical.Result.XP);
                        }
                        foreach(var historical in new[]{storage.LoadHolon(id,loadChildren:false,version:1),await storage.LoadHolonAsync(id,loadChildren:false,version:1)}){
                            Assert.False(historical.IsError,historical.Message);Assert.Equal(1,historical.Result.Version);Assert.Equal(originalName,historical.Result.Name);
                        }
                    }
                    foreach(IOASISStorageProvider storage in new IOASISStorageProvider[]{cosmos,local}){
                        Assert.Equal(2,(await storage.LoadAvatarAsync(avatarId)).Result.Version);
                        Assert.Equal("updated",(await storage.LoadAvatarAsync(avatarId)).Result.MetaData["version-proof"].ToString());
                        Assert.Equal(2,(await storage.LoadAvatarDetailAsync(avatarId)).Result.Version);
                        Assert.Equal(73,(await storage.LoadAvatarDetailAsync(avatarId)).Result.XP);
                        Assert.Equal(2,(await storage.LoadHolonAsync(id,loadChildren:false)).Result.Version);
                    }
                }
                NativeNearbyAcceptance.Verify((IOASISNETProvider)cosmos,avatarId,id,true);
                NativeNearbyAcceptance.RejectInvalidInputs((IOASISNETProvider)cosmos);
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
                    Assert.Equal(versioned?13:5,(await store.GetAsync(execution.OperationId,token)).ProviderAttempts.Count(attempt=>attempt.Provider=="CosmosBlockChainOASIS"));
                }finally{
                    dna.OASIS.SubscriptionConfig.PlanType="Gold";manager.IsAutoLoadBalanceEnabled=false;manager.IsAutoReplicationEnabled=true;
                }
                if(!nativePrimary&&synchronous){
                    Assert.False(manager.SetAndReplaceAutoFailOverListForProviders(providers.Select(type=>new EnumValue<ProviderType>(type))).IsError);
                    manager.IsAutoFailOverEnabled=true;
                    var nativeBefore=manager.PerformanceMonitor.GetMetrics(ProviderType.CosmosBlockChainOASIS).TotalRequests;
                    var localBefore=manager.PerformanceMonitor.GetMetrics(ProviderType.LocalFileOASIS).TotalRequests;
                    await ControlNodeAsync("stop-owned-cosmos-test-node.sh");
                    Exception? outageFailure=null;
                    try{
                        void CheckFailover(){
                            var diagnostic=manager.LastFailoverDiagnostic;
                            Assert.True(diagnostic.Succeeded);Assert.False(diagnostic.Exhausted);Assert.False(diagnostic.WasQuotaBlocked);
                            Assert.Equal(ProviderType.CosmosBlockChainOASIS,diagnostic.OriginalProvider);
                            Assert.Equal(ProviderType.LocalFileOASIS,diagnostic.SelectedProvider);
                            Assert.Equal(2,diagnostic.Attempts.Count);
                            Assert.Equal(ProviderType.CosmosBlockChainOASIS,diagnostic.Attempts[0].Provider);Assert.False(diagnostic.Attempts[0].Succeeded);
                            Assert.Equal(ProviderType.LocalFileOASIS,diagnostic.Attempts[1].Provider);Assert.True(diagnostic.Attempts[1].Succeeded);
                            outageProfileReadCount++;nonNativeReads++;
                        }
                        foreach(var suffix in new[]{"","ByProviderKey","ByUsername","ByEmail"})foreach(var sync in new[]{false,true}){
                            var request=new StorageOperationRequest{Operation="LoadAvatar"+suffix,AvatarId=avatarId,ProviderKey=avatarId.ToString(),Username=avatar.Username,Email=avatar.Email,PreferredProvider=ProviderType.CosmosBlockChainOASIS};
                            var read=sync?router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
                            Assert.False(read.IsError,read.Message);Assert.Equal(avatarId,read.Result?.Id);Assert.Equal(avatar.Username,read.Result?.Username);CheckFailover();
                            if(suffix=="ByProviderKey")continue;
                            request.Operation="LoadAvatarDetail"+suffix;
                            var profile=sync?router.RouteRequest<IAvatarDetail>(request):await router.RouteRequestAsync<IAvatarDetail>(request);
                            Assert.False(profile.IsError,profile.Message);Assert.Equal(avatarId,profile.Result?.Id);Assert.Equal(avatar.Email,profile.Result?.Email);CheckFailover();
                        }
                        Assert.Equal(14,outageProfileReadCount);
                        Assert.Equal(nativeBefore+14,manager.PerformanceMonitor.GetMetrics(ProviderType.CosmosBlockChainOASIS).TotalRequests);
                        Assert.Equal(localBefore+14,manager.PerformanceMonitor.GetMetrics(ProviderType.LocalFileOASIS).TotalRequests);
                    }catch(Exception failure){outageFailure=failure;}
                    try{
                        await ResumeNodeAsync(rpc,chain,token);
                    }catch(Exception restoreFailure){if(outageFailure!=null)throw new AggregateException(outageFailure,restoreFailure);throw;}
                    finally{manager.IsAutoFailOverEnabled=false;}
                    if(outageFailure!=null)ExceptionDispatchInfo.Capture(outageFailure).Throw();
                }
                var remove=new StorageOperationRequest{Operation="DeleteHolon",HolonId=id,SoftDelete=false,PreferredProvider=preferred,LoadChildren=false};
                var deleted=synchronous?router.RouteRequest<IHolon>(remove):await router.RouteRequestAsync<IHolon>(remove);
                Assert.False(deleted.IsError,deleted.Message);Assert.False(deleted.IsWarning,string.Join("\n",deleted.InnerMessages));
                Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                Assert.Null((await cosmos.LoadHolonAsync(id,loadChildren:false)).Result);
                Assert.Null((await local.LoadHolonAsync(id,loadChildren:false)).Result);
                var removeProfile=new StorageOperationRequest{Operation="DeleteAvatar",AvatarId=avatarId,SoftDelete=false,PreferredProvider=preferred};
                var deletedProfile=synchronous?router.RouteRequest<bool>(removeProfile):await router.RouteRequestAsync<bool>(removeProfile);
                Assert.False(deletedProfile.IsError,deletedProfile.Message);Assert.False(deletedProfile.IsWarning,deletedProfile.Message);Assert.True(deletedProfile.Result);
                Assert.Equal(1,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
                await ReadProfiles(true);
                if(versioned)foreach(IOASISStorageProvider storage in new IOASISStorageProvider[]{cosmos}){
                    Assert.Null((await storage.LoadAvatarAsync(avatarId,1)).Result);
                    Assert.Null((await storage.LoadAvatarDetailAsync(avatarId,1)).Result);
                    Assert.Null((await storage.LoadHolonAsync(id,loadChildren:false,version:1)).Result);
                }
                NativeNearbyAcceptance.Verify((IOASISNETProvider)cosmos,avatarId,id,false);
                return true;
            });
            // Recovery gets its own bounded lifetime after execution; an expired scenario
            // token must not prevent accounting for transactions already submitted.
            using var recoveryDeadline=new CancellationTokenSource(TimeSpan.FromSeconds(180));
            var recoveryToken=recoveryDeadline.Token;
            var operation=await new MongoUsageOutboxStore(connection,database).GetAsync(execution.OperationId,recoveryToken);
            var native=operation.ProviderAttempts.Where(attempt=>attempt.Provider=="CosmosBlockChainOASIS").ToArray();
            var expectedOriginals=versioned?21:10;
            Assert.Equal(expectedOriginals,native.Length);Assert.Equal((versioned?8:5)+nonNativeReads,operation.ProviderAttempts.Count(attempt=>attempt.Provider=="LocalFileOASIS"));
            Assert.Equal(native.Sum(attempt=>NativeFeeMaximumAccounting.ValueUsd(attempt.NativePreparation.FeeMaximums,attempt.NativePreparation.PriceQuotes)),operation.NativeFeeExposureUsd);
            var resolver=new PreparedNativeUsageRecoveryResolver(store,new[]{feeReader});Assert.True(resolver.CanResolve(operation));
            // Recovery deliberately bounds each pass to16 originals. Drive enough
            // read-only passes, never raise the global bound or replay mutations.
            for(var pass=0;pass<(expectedOriginals+15)/16;pass++){
                Assert.True(resolver.CanResolve(operation));Assert.Null(await resolver.ResolveAsync(operation,recoveryToken));
                operation=await store.GetAsync(execution.OperationId,recoveryToken);
                Assert.Equal(Math.Min((pass+1)*16,expectedOriginals),operation.Receipts.Count);
            }
            var originals=new List<object>();
            var netUnits=BigInteger.Zero;
            foreach(var attempt in native){
                Assert.Null(attempt.AcknowledgedAtUtc);
                var receipt=Assert.Single(operation.Receipts.Where(receipt=>receipt.ProviderRequestId==attempt.ProviderRequestId));
                Assert.NotNull(receipt.NativeFeeAudit);receipt.NativeFeeAudit.Validate(attempt.Provider,attempt.ProviderRequestId,chain);
                Assert.Equal(review.EvidenceBasis,receipt.CostSource);
                using var audit=JsonDocument.Parse(receipt.NativeFeeAudit.PayloadJson);
                Assert.True(audit.RootElement.GetProperty("NativeExecutionSucceeded").GetBoolean());Assert.Equal(0,audit.RootElement.GetProperty("NativeExecutionCode").GetInt32());
                Assert.InRange(audit.RootElement.GetProperty("ReceiptHeight").GetInt64(),review.FirstHeight,review.LastHeight);
                foreach(var value in receipt.NativeFeeValuations){Assert.Equal("stake",value.Denomination);netUnits+=BigInteger.Parse(value.NativeNetUnits,System.Globalization.CultureInfo.InvariantCulture);}
                originals.Add(new{attempt,receipt});
            }
            Assert.Equal(netUnits,walletBefore-await Balance(recoveryToken));Assert.False(resolver.CanResolve(operation));Assert.Null(operation.ObservedOutcome);
            output.WriteLine(JsonSerializer.Serialize(new{nativePrimary,synchronous,versioned,holonId=id,avatarId,operation.NativeFeeExposureUsd,originals,netFinalNativeReceiptCount=expectedOriginals,walletDebit=netUnits.ToString(),review,
                historicalReadCount=versioned?6:0,currentVersionReadCount=versioned?10:0,historicalAbsenceReadCount=versioned?3:0,
                nearbyNativeReadCount=18,nearbyInvalidInputCount=4,profileMatchingReadCount=28,profileAbsenceReadCount=28,outageProfileReadCount,selectorReadCount=16,strategyReadCount=2*Enum.GetValues<LoadBalancingStrategy>().Length,roundRobinReadCount=6,freeReadCount=Enum.GetValues<LoadBalancingStrategy>().Length,nonNativeReads,hostedSettlementVerified=false}));
            Assert.Equal(oldHash,Hash(oldPath));Assert.Null(operation.Settlement);complete=true;
        }catch(Exception){
            output.WriteLine(JsonSerializer.Serialize(new{lastReplication=manager.LastReplicationDiagnostic,lastFailover=manager.LastFailoverDiagnostic}));
            throw;
        }finally{
            if(complete){file.Dispose();File.Delete(journal);}
            else Console.WriteLine("Preserved independent routing journal/outbox: "+journal);
            await local.DeActivateProviderAsync();
        }
    }

    internal static async Task ControlNodeAsync(string script)
    {
        Assert.Contains(script,new[]{"stop-owned-cosmos-test-node.sh","resume-owned-cosmos-test-node.sh"});
        var start=new ProcessStartInfo("wsl.exe"){UseShellExecute=false,CreateNoWindow=true};
        foreach(var argument in new[]{"-d","Ubuntu","-u","root","--","bash","/mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/"+script})start.ArgumentList.Add(argument);
        using var process=Process.Start(start)!;
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));Assert.Equal(0,process.ExitCode);
    }

    internal static async Task ResumeNodeAsync(string rpc,string chain,CancellationToken token)
    {
        await ControlNodeAsync("resume-owned-cosmos-test-node.sh");
        using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(2)};
        var readyDeadline=DateTime.UtcNow.AddSeconds(30);var ready=false;long? firstObservedHeight=null;
        while(DateTime.UtcNow<readyDeadline){
            try{
                using var response=JsonDocument.Parse(await http.GetStringAsync(rpc+"/status",token));
                Assert.Equal(chain,response.RootElement.GetProperty("result").GetProperty("node_info").GetProperty("network").GetString());
                var sync=response.RootElement.GetProperty("result").GetProperty("sync_info");
                var height=long.Parse(sync.GetProperty("latest_block_height").GetString()!,System.Globalization.CultureInfo.InvariantCulture);
                firstObservedHeight??=height;
                // SDK0.54 simulation can have an empty check-state block time after restart even while
                // RPC status and committed-state reads work. Observe a NEW committed block before signing.
                if(!sync.GetProperty("catching_up").GetBoolean()&&height>firstObservedHeight.Value){ready=true;break;}
            }catch(HttpRequestException){}catch(TaskCanceledException)when(!token.IsCancellationRequested){}
            await Task.Delay(200,token);
        }
        Assert.True(ready,"Retained Cosmos node must commit a fresh block after resume before simulation/prepared cleanup.");
    }

    internal sealed class TestAuthority(string user):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
            var body=await request.Content!.ReadAsStringAsync(token);object result;
            if(request.RequestUri!.AbsolutePath.EndsWith("authorize")){var input=JsonSerializer.Deserialize<UsageAuthorizationRequest>(body)!;
                result=new SubscriptionAuthorizationResult{Allowed=true,StatusCode=200,UserId=user,OperationId=input.OperationId,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(5)};
            }else if(request.RequestUri.AbsolutePath.EndsWith("start")){var input=JsonSerializer.Deserialize<UsageStartRequest>(body)!;result=new UsageStartResult{Started=true,OperationId=input.OperationId,Status="executing"};}
            else throw new InvalidOperationException("Unexpected test-authority request.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(result))};
        }
    }
}
