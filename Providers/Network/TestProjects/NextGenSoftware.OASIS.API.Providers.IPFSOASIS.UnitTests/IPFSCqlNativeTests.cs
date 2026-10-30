using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public class IPFSCqlNativeTests
{
    [Fact,Trait("Category","IPFSCqlNative")]
    public Task ActualKuboPointThroughManagerLinqAndDemoMatchesCanonicalRead()=>RunAsync(false);

    [Fact,Trait("Category","IPFSCqlWeb4Native")]
    public Task ActualKuboPointThroughWeb4RowsAndMeasuredReceipt()=>RunAsync(true);

    [Fact,Trait("Category","IPFSCqlInteractiveNative")]
    public Task ActualKuboInteractivePointHost()=>RunAsync(true,true);

    [Fact,Trait("Category","IPFSCqlKeySetNative")]
    public Task ActualKuboCollectionSelectsSeveralEncryptedRecords()=>RunAsync(false,keySet:true);

    private async Task RunAsync(bool web4,bool interactive=false,bool keySet=false)
    {
        const string endpoint="http://127.0.0.1:18547";
        Assert.Equal(endpoint,Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC"));
        var state=Path.GetDirectoryName(Environment.GetEnvironmentVariable("IPFS_PATH"))!;
        Assert.True(File.Exists(Path.Combine(state,"repo","config")));
        var prefix=keySet?"cql-keyset":interactive?"cql-interactive":web4?"cql-web4":"cql-point";
        var dnaPath=Path.Combine(state,prefix+"-dna.json");
        var pending=Path.Combine(state,prefix+".pending.json");
        var closed=Path.Combine(state,prefix+"-complete.json");
        Assert.False(File.Exists(dnaPath)||File.Exists(pending)||File.Exists(pending+".next")||File.Exists(closed),"Preserve prior native CQL fixture; never replay an unknown write.");
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings {
                LogSwitchingProviders=false,IPFSOASIS=new IPFSOASISSettings {ConnectionString=endpoint}}}};
        var provider=new IPFSOASIS(dna,dnaPath) { EnableCqlCidKeySets=keySet };
        var owner=Guid.NewGuid();var root=new Holon {Id=Guid.NewGuid(),CreatedByAvatarId=owner,IsPublic=true,Name="CQL IPFS native demo"};
        var child=new Holon {Id=Guid.NewGuid(),CreatedByAvatarId=owner};root.Children.Add(child);
        var extra=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,IsPublic=true,Name="Third encrypted CQL record"};
        if(keySet)root.Children.Add(extra);
        var ownedIds=keySet?new[]{root.Id,child.Id,extra.Id}:new[]{root.Id,child.Id};
        string phase="before-save";string? cid=null;
        async Task Checkpoint(){
            await using(var file=new FileStream(pending+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(file,new {phase,owner,rootId=root.Id,childId=child.Id,ownedIds,recordCid=cid,dnaPath});file.Flush(true);
            }
            File.Move(pending+".next",pending,true);
        }
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try{
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            await Checkpoint();
            var saved=await provider.SaveHolonAsync(root);Assert.False(saved.IsError,saved.Message);Assert.Equal(ownedIds.Length,saved.SavedCount);
            cid=root.ProviderUniqueStorageKey[ProviderType.IPFSOASIS];phase="saved";await Checkpoint();
            var independent=await provider.LoadHolonAsync(cid,loadChildren:false);
            Assert.False(independent.IsError);Assert.Equal(root.Id,independent.Result.Id);Assert.Equal(root.Name,independent.Result.Name);
            if(keySet)
            {
                var keys=new List<string>();
                foreach(var ownedId in ownedIds)
                {
                    var canonical=await provider.LoadHolonAsync(ownedId,loadChildren:false);
                    Assert.False(canonical.IsError,canonical.Message);
                    keys.Add(canonical.Result.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
                }
                // Native IN filter over three stored encrypted holons; preserve exact source identities.
                var selected=keys.ToArray();
                var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.IPFSOASIS).Result)
                    .Where(row=>selected.Contains(CosmicQueryMetadata.Value<string>(row,IPFSOASIS.CqlRecordCidKey))).Result
                    .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(owner).Result).Result;
                var plan=CosmicQueryPlanner.Plan(query,ProviderType.IPFSOASIS,
                    new[]{new CosmicQueryTarget(ProviderType.IPFSOASIS,provider.QueryCapabilities)});
                Assert.False(plan.IsError,plan.Message);
                var collection=await provider.ExecuteCosmicQueryAsync(plan.Result,cancellationToken:timeout.Token);
                Assert.False(collection.IsError,collection.Exception?.ToString());
                Assert.Equal(ownedIds,collection.Result.Select(row=>row.Id));
                Assert.Equal(selected,collection.Result.Select(row=>(string)row.MetaData[IPFSOASIS.CqlRecordCidKey]));
                var subset=selected.Take(2).ToArray();
                var filtered=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.IPFSOASIS).Result)
                    .Where(row=>subset.Contains(CosmicQueryMetadata.Value<string>(row,IPFSOASIS.CqlRecordCidKey))).Result;
                var subsetPlan=CosmicQueryPlanner.Plan(filtered,ProviderType.IPFSOASIS,
                    new[]{new CosmicQueryTarget(ProviderType.IPFSOASIS,provider.QueryCapabilities)});
                Assert.False(subsetPlan.IsError,subsetPlan.Message);
                var subsetRows=await provider.ExecuteCosmicQueryAsync(subsetPlan.Result,cancellationToken:timeout.Token);
                Assert.False(subsetRows.IsError,subsetRows.Message);Assert.Equal(ownedIds.Take(2),subsetRows.Result.Select(row=>row.Id));
                var nativeDna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
                    StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
                var nativeRegistry=new ProviderManager(null,nativeDna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
                if(Environment.GetEnvironmentVariable("OASIS_PROTOCOL_MONGO_ROOT") is string mongoRoot)
                    await NextGenSoftware.OASIS.Providers.TestSupport.NativeProtocolMongoAcceptance.RunAsync(
                        mongoRoot,provider,selected,owner,nativeDna,nativeRegistry);
                await NextGenSoftware.OASIS.Providers.TestSupport.ProtocolPointWeb4Acceptance.VerifyKeySetTeachingHostAsync(
                    provider,new HolonManager(provider,nativeDna,nativeRegistry),owner,
                    selected.Select((key,index)=>new{key,id=ownedIds[index]}).ToDictionary(pair=>pair.key,pair=>pair.id));
                phase="native-keyset-verified";await Checkpoint();
            }
            else
            {
            var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
            var manager=new HolonManager(provider,dna,registry);
            using var output=new StringWriter();
            var rows=await CosmicCqlDemo.IpfsPointWalkthrough.RunAsync(manager,provider,cid,Guid.NewGuid(),output,timeout.Token);
            var row=Assert.Single(rows);Assert.Equal(independent.Result.Id,row.Id);Assert.Equal(independent.Result.Name,row.Name);
            Assert.Equal(cid,row.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);Assert.Empty(row.Children);
            Assert.Contains("Returned holons: 1",output.ToString());phase="demo-verified";await Checkpoint();

            if(interactive){
                await NextGenSoftware.OASIS.Providers.TestSupport.ProtocolPointWeb4Acceptance.VerifyTeachingHostAsync(provider,manager,owner,root.Id,cid);
                phase="interactive-verified";await Checkpoint();
            }
            else if(web4){
                await NextGenSoftware.OASIS.Providers.TestSupport.ProtocolPointWeb4Acceptance.RunAsync(
                    provider,manager,owner,"IPFSRecordCid",cid,"IPFSRecordCid",cid,root.Id);
                phase="web4-verified";await Checkpoint();
            }

            }

            // Exact owned fixture maintenance, after native query/read proof. Never erase historical blocks.
            var catalogue=await provider.LoadLookupToJson();Assert.Equal(ownedIds.Length,catalogue.Count);
            Assert.True(ownedIds.ToHashSet().SetEquals(catalogue.Values.Select(x=>x.Id)));
            Assert.All(catalogue.Values,x=>Assert.Equal("holon",x.RecordKind));
            foreach(var id in ownedIds){
                var owned=await provider.LoadHolonAsync(id,loadChildren:false);Assert.False(owned.IsError);
                Assert.Equal(owner,owned.Result.CreatedByAvatarId);
            }
            phase="before-exact-cleanup";await Checkpoint();
            await provider.SaveLookupToFile(new Dictionary<string,HolonResume>());Assert.Empty(await provider.LoadLookupToJson());
            Assert.Null((await provider.LoadHolonAsync(root.Id,loadChildren:false)).Result);
            Assert.Null((await provider.LoadHolonAsync(child.Id,loadChildren:false)).Result);
            if(keySet)Assert.Null((await provider.LoadHolonAsync(extra.Id,loadChildren:false)).Result);
            phase="complete";await Checkpoint();File.Move(pending,closed);
        }
        finally{await provider.DeActivateProviderAsync();OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;}
    }
}
