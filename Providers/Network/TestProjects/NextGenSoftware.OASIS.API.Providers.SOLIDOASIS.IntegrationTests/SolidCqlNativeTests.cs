using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
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

    private async Task RunAsync(bool web4,bool interactive=false)
    {
        var original=Environment.GetEnvironmentVariable("OASIS_SOLID_COLD_READONLY_WORK")!;
        var prefix=interactive?"cql-interactive":web4?"cql-web4":"cql-point";
        var pending=Path.Combine(original,prefix+".pending.json");var closed=Path.Combine(original,prefix+"-complete.json");
        Assert.False(File.Exists(pending)||File.Exists(pending+".next")||File.Exists(closed),"Preserve native CQL ownership before rerunning.");
        const string keyVariable="OASIS_SOLID_CQL_KEY",secretVariable="OASIS_SOLID_CQL_SECRET";
        var priorKey=Environment.GetEnvironmentVariable(keyVariable);var priorSecret=Environment.GetEnvironmentVariable(secretVariable);
        byte[]? key=null,credentials=null,pod=null;
        var owner=Guid.NewGuid();var record=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,IsPublic=false,Name="SOLID CQL native demo"};
        var phase="before-save";
        async Task Checkpoint(){
            await using(var file=new FileStream(pending+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(file,new{phase,owner,recordId=record.Id});file.Flush(true);
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
                    ClientSecretEnvironmentVariable=secretVariable,StorageKeyEnvironmentVariable=keyVariable});

            using(provider){
                Assert.False((await provider.ActivateProviderAsync()).IsError);
                await Checkpoint();
                var saved=await provider.SaveHolonAsync(record,saveChildren:false);Assert.False(saved.IsError,saved.Message);Assert.Equal(1,saved.SavedCount);
                phase="saved";await Checkpoint();
                var canonical=await provider.LoadHolonAsync(record.Id,loadChildren:false);Assert.False(canonical.IsError);
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

                // Conditional deletion of only this exact independently verified owned fixture.
                Assert.Equal(owner,canonical.Result.CreatedByAvatarId);phase="before-cleanup";await Checkpoint();
                var removed=await provider.DeleteHolonAsync(record.Id);Assert.False(removed.IsError);Assert.True(removed.IsDeleted);Assert.Equal(1,removed.DeletedCount);
                var absent=await provider.LoadHolonAsync(record.Id,loadChildren:false);Assert.True(absent.IsError);Assert.Null(absent.Result);
                Assert.Equal(System.Net.HttpStatusCode.NotFound,Assert.IsType<HttpRequestException>(absent.Exception).StatusCode);
                phase="complete";await Checkpoint();File.Move(pending,closed);
            }
        }finally{
            Environment.SetEnvironmentVariable(keyVariable,priorKey);Environment.SetEnvironmentVariable(secretVariable,priorSecret);
            if(key!=null)CryptographicOperations.ZeroMemory(key);if(credentials!=null)CryptographicOperations.ZeroMemory(credentials);if(pod!=null)CryptographicOperations.ZeroMemory(pod);
            OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;
        }
    }
}
