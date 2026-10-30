using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using Provider=NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.SOLIDOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests;

public class SolidCqlNativeTests
{
    [Fact,Trait("Category","SolidCqlNative")]
    public Task ActualPrivatePodPointThroughManagerLinqAndDemoMatchesCanonicalRead()=>RunAsync(false);

    [Fact,Trait("Category","SolidCqlWeb4Native")]
    public Task ActualPrivatePodPointThroughWeb4RowsAndMeasuredReceipt()=>RunAsync(true);

    [Fact,Trait("Category","SolidCqlInteractiveNative")]
    public Task ActualPrivatePodInteractivePointHost()=>RunAsync(true,true);

    [Fact,Trait("Category","SolidCqlKeySetNative")]
    public Task ActualPrivatePodCollectionReturnsSelectedEncryptedResources()=>RunAsync(false,keySet:true);

    private async Task RunAsync(bool web4,bool interactive=false,bool keySet=false)
    {
        var original=Environment.GetEnvironmentVariable("OASIS_SOLID_COLD_READONLY_WORK")!;
        var prefix=keySet?"cql-keyset":interactive?"cql-interactive":web4?"cql-web4":"cql-point";
        var pending=Path.Combine(original,prefix+".pending.json");var closed=Path.Combine(original,prefix+"-complete.json");
        Assert.False(File.Exists(pending)||File.Exists(pending+".next")||File.Exists(closed),"Preserve native CQL ownership before rerunning.");
        const string keyVariable="OASIS_SOLID_CQL_KEY",secretVariable="OASIS_SOLID_CQL_SECRET";
        var priorKey=Environment.GetEnvironmentVariable(keyVariable);var priorSecret=Environment.GetEnvironmentVariable(secretVariable);
        byte[]? key=null,credentials=null,pod=null;
        var owner=Guid.NewGuid();var record=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,IsPublic=false,Name="SOLID CQL native demo"};
        var records=keySet?new[]{record,
            new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,Name="Second private CQL record"},
            new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,Name="Third private CQL record"}}:new[]{record};
        var phase="before-save";
        async Task Checkpoint(){
            await using(var file=new FileStream(pending+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(file,new{phase,owner,recordId=record.Id,ownedIds=records.Select(row=>row.Id).ToArray()});file.Flush(true);
            }
            File.Move(pending+".next",pending,true);
        }
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        try{
            Provider provider;
                using var manifest=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(original,"environment.json")));
                var podName=manifest.RootElement.GetProperty("demoPodName").GetString()!;
                Assert.Matches("^[a-z][a-z0-9-]{0,39}$",podName);
                var podRoot=new Uri(new Uri("http://localhost:13000/"),podName+"/");
                var keyPath=Path.Combine(original,"storage-key.dpapi");
                var clientPath=Path.Combine(original,"private-local-client.dpapi");
                var podPath=Path.Combine(original,"private-local-pod.dpapi");
                if(!File.Exists(keyPath)||!File.Exists(clientPath)||!File.Exists(podPath))
                    throw new InvalidOperationException("Original protected SOLID key and identity are required; never replace them over retained records.");
                key=ProtectedData.Unprotect(await File.ReadAllBytesAsync(keyPath),null,DataProtectionScope.CurrentUser);
                credentials=ProtectedData.Unprotect(await File.ReadAllBytesAsync(clientPath),null,DataProtectionScope.CurrentUser);
                pod=ProtectedData.Unprotect(await File.ReadAllBytesAsync(podPath),null,DataProtectionScope.CurrentUser);
                using var client=JsonDocument.Parse(credentials);
                using var originalPod=JsonDocument.Parse(pod);
                if(!podRoot.IsBaseOf(new Uri(originalPod.RootElement.GetProperty("webId").GetString()!)))
                    throw new InvalidOperationException("Retained identity does not match the fixed private test Pod.");
                Environment.SetEnvironmentVariable(keyVariable,Convert.ToBase64String(key));
                Environment.SetEnvironmentVariable(secretVariable,client.RootElement.GetProperty("secret").GetString());
                provider=SolidProviderFactory.Create(new SolidOASISProviderSettings{
                    PodRoot=podRoot.AbsoluteUri,AuthenticationMode="DpopClientCredentials",
                    TokenEndpoint="http://localhost:13000/.oidc/token",ClientId=client.RootElement.GetProperty("id").GetString(),
                    ClientSecretEnvironmentVariable=secretVariable,StorageKeyEnvironmentVariable=keyVariable},enableCqlRecordKeySets:keySet);

            using(provider){
                Assert.False((await provider.ActivateProviderAsync()).IsError);
                await Checkpoint();
                foreach(var item in records){
                    var saved=await provider.SaveHolonAsync(item,saveChildren:false);Assert.False(saved.IsError,saved.Message);Assert.Equal(1,saved.SavedCount);
                }
                phase="saved";await Checkpoint();
                var canonical=await provider.LoadHolonAsync(record.Id,loadChildren:false);Assert.False(canonical.IsError);
                if(keySet)
                {
                    var keys=records.Select(row=>row.Id.ToString("D")).ToArray();
                    async Task<IReadOnlyList<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>> Read(string[] selected,Guid actor)
                    {
                        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SOLIDOASIS).Result)
                            .Where(row=>selected.Contains(CosmicQueryMetadata.Value<string>(row,Provider.CqlRecordIdKey))).Result
                            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
                        var plan=CosmicQueryPlanner.Plan(query,ProviderType.SOLIDOASIS,
                            new[]{new CosmicQueryTarget(ProviderType.SOLIDOASIS,provider.QueryCapabilities)});
                        Assert.False(plan.IsError,plan.Message);
                        var result=await provider.ExecuteCosmicQueryAsync(plan.Result);
                        Assert.False(result.IsError,result.Exception?.ToString());return result.Result;
                    }
                    var all=await Read(keys,owner);Assert.Equal(records.Select(row=>row.Id),all.Select(row=>row.Id));
                    var subset=await Read(keys.Take(2).ToArray(),owner);
                    Assert.Equal(records.Take(2).Select(row=>row.Id),subset.Select(row=>row.Id));
                    Assert.Empty(await Read(keys,Guid.NewGuid()));
                    foreach(var row in all){
                        var source=await provider.LoadHolonAsync(row.Id,loadChildren:false);Assert.False(source.IsError);
                        Assert.Equal(source.Result.Name,row.Name);Assert.Equal(owner,source.Result.CreatedByAvatarId);
                        Assert.Equal(row.Id.ToString("D"),row.MetaData[Provider.CqlRecordIdKey]);
                        Assert.True(podRoot.IsBaseOf(new Uri((string)row.MetaData["SolidRecordResource"])));
                        Assert.True(row.MetaData.ContainsKey("SolidSourceETag"));Assert.Empty(row.Children);
                    }
                    var nativeDna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
                        StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
                    var nativeRegistry=new ProviderManager(null,nativeDna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
                    if(Environment.GetEnvironmentVariable("OASIS_PROTOCOL_MONGO_ROOT") is string mongoRoot)
                    await NextGenSoftware.OASIS.Providers.TestSupport.NativeProtocolMongoAcceptance.RunAsync(
                        mongoRoot,provider,keys,owner,nativeDna,nativeRegistry);
                await NextGenSoftware.OASIS.Providers.TestSupport.ProtocolPointWeb4Acceptance.VerifyKeySetTeachingHostAsync(
                        provider,new HolonManager(provider,nativeDna,nativeRegistry),owner,
                        records.ToDictionary(row=>row.Id.ToString("D"),row=>row.Id));
                    phase="native-keyset-verified";await Checkpoint();
                }
                else
                {
                var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,
                    StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
                var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
                var manager=new HolonManager(provider,dna,registry);
                using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));using var output=new StringWriter();
                var rows=await CosmicCqlDemo.SolidPointWalkthrough.RunAsync(manager,provider,record.Id.ToString("D"),owner,output,timeout.Token);
                var row=Assert.Single(rows);Assert.Equal(canonical.Result.Id,row.Id);Assert.Equal(canonical.Result.Name,row.Name);Assert.Empty(row.Children);
                Assert.Equal(record.Id.ToString(),row.ProviderUniqueStorageKey[ProviderType.SOLIDOASIS]);
                var resource=new Uri((string)row.MetaData["SolidRecordResource"]);Assert.True(podRoot.IsBaseOf(resource));
                Assert.True(row.MetaData.ContainsKey("SolidSourceETag"));Assert.Contains("Returned holons: 1",output.ToString());
                using var anonymous=new HttpClient{Timeout=TimeSpan.FromSeconds(10)};
                using var denied=await anonymous.GetAsync(resource,timeout.Token);
                Assert.True(denied.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden);
                var hidden=await CosmicCqlDemo.SolidPointWalkthrough.RunAsync(manager,provider,record.Id.ToString("D"),Guid.NewGuid(),TextWriter.Null,timeout.Token);
                Assert.Empty(hidden);phase="query-verified";await Checkpoint();

                if(interactive){
                    await NextGenSoftware.OASIS.Providers.TestSupport.ProtocolPointWeb4Acceptance.VerifyTeachingHostAsync(provider,manager,owner,record.Id,record.Id.ToString("D"));
                    phase="interactive-verified";await Checkpoint();
                }
                else if(web4){
                    await NextGenSoftware.OASIS.Providers.TestSupport.ProtocolPointWeb4Acceptance.RunAsync(
                        provider,manager,owner,"SolidRecordId",record.Id.ToString("D"),"SolidRecordResource",resource.AbsoluteUri,record.Id);
                    phase="web4-verified";await Checkpoint();
                }

                }

                // Conditional deletion of only this exact independently verified owned fixture.
                Assert.Equal(owner,canonical.Result.CreatedByAvatarId);phase="before-cleanup";await Checkpoint();
                foreach(var item in records){
                var owned=await provider.LoadHolonAsync(item.Id,loadChildren:false);
                Assert.False(owned.IsError);Assert.Equal(owner,owned.Result.CreatedByAvatarId);
                var removed=await provider.DeleteHolonAsync(item.Id);Assert.False(removed.IsError);Assert.True(removed.IsDeleted);Assert.Equal(1,removed.DeletedCount);
                var absent=await provider.LoadHolonAsync(item.Id,loadChildren:false);Assert.True(absent.IsError);Assert.Null(absent.Result);
                Assert.Equal(System.Net.HttpStatusCode.NotFound,Assert.IsType<HttpRequestException>(absent.Exception).StatusCode);
                }
                phase="complete";await Checkpoint();File.Move(pending,closed);
            }
        }finally{
            Environment.SetEnvironmentVariable(keyVariable,priorKey);Environment.SetEnvironmentVariable(secretVariable,priorSecret);
            if(key!=null)CryptographicOperations.ZeroMemory(key);if(credentials!=null)CryptographicOperations.ZeroMemory(credentials);if(pod!=null)CryptographicOperations.ZeroMemory(pod);
            OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;
        }
    }
}
