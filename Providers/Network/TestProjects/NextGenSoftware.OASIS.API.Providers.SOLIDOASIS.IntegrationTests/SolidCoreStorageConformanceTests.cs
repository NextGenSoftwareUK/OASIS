using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Provider=NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.SOLIDOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests;

public class SolidCoreStorageConformanceTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    [Trait("Category","SolidCoreStorageConformance")]
    public async Task SharedStorageInterfaceAgainstRetainedPrivatePod(bool sync)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.NotNull(root);
        var work=Path.Combine(root!.FullName,"TestResults","ProviderConformanceCurrent","SOLIDOASIS",sync?"sync":"async");
        Directory.CreateDirectory(work);var path=Path.Combine(work,"report.json");
        var original=Environment.GetEnvironmentVariable("OASIS_SOLID_COLD_READONLY_WORK")
            ??throw new InvalidOperationException("Select the persistent owned SOLID environment explicitly.");
        var journal=Path.Combine(original,"core-"+(sync?"sync":"async")+".pending.json");
        Assert.False(File.Exists(journal)||File.Exists(journal+".next"),"Preserve original pending fixture; never replay writes.");
        Assert.False(File.Exists(path)||File.Exists(path+".next"),"Preserve original conformance evidence before another native run.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using(var intent=new FileStream(journal+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(intent,new { report.OwnerId,report.RootId,report.ChildId,report.ImportId,report.BatchIds,
                    report.CurrentCase,report.Passed,report.Succeeded,report.FixtureCleanupComplete });intent.Flush(true);
            }
            File.Move(journal+".next",journal,true);
            await using(var file=new FileStream(path+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(file,report);file.Flush(true);
            }
            File.Move(path+".next",path,overwrite:true);
        }
        const string keyVariable="OASIS_SOLID_CORE_STORAGE_KEY",secretVariable="OASIS_SOLID_CORE_CLIENT_SECRET";
        var priorKey=Environment.GetEnvironmentVariable(keyVariable);var priorSecret=Environment.GetEnvironmentVariable(secretVariable);
        byte[]? key=null,credentials=null,pod=null;
        try{
            Provider provider;
            try{
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
            }catch(Exception cause){
                await Checkpoint(new CoreStorageConformance.Report{Provider="SOLIDOASIS",Mode="native",Synchronous=sync,
                    CurrentCase="configuration",FailureType=cause.GetType().FullName!});
                Assert.True(false,"Original SOLID configuration unavailable; no storage operation dispatched.");return;
            }
            using(provider){
                var result=await CoreStorageConformance.RunAsync(provider,"SOLIDOASIS","native",sync,Checkpoint,expectedDefaultDeletion:"absent");
                Assert.True(result.Succeeded,result.CurrentCase+": "+result.FailureType);
                Assert.True(result.FixtureCleanupComplete);
                File.Delete(journal);
            }
        }finally{
            Environment.SetEnvironmentVariable(keyVariable,priorKey);Environment.SetEnvironmentVariable(secretVariable,priorSecret);
            if(key!=null)CryptographicOperations.ZeroMemory(key);
            if(credentials!=null)CryptographicOperations.ZeroMemory(credentials);
            if(pod!=null)CryptographicOperations.ZeroMemory(pod);
        }
    }

    [Fact,Trait("Category","SolidCoreFixtureCleanup")]
    public async Task ReconcileExactOriginalDeletedChildrenAndOwnedRemainingRecords()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.NotNull(root);
        var original=Environment.GetEnvironmentVariable("OASIS_SOLID_COLD_READONLY_WORK")!;
        var pending=Path.Combine(original,"core-cleanup.pending.json");
        var closed=Path.Combine(original,"core-cleanup-complete.json");
        Assert.False(File.Exists(pending)||File.Exists(pending+".next")||File.Exists(closed),"Never replay cleanup.");
        const string keyVariable="OASIS_SOLID_CORE_STORAGE_KEY",secretVariable="OASIS_SOLID_CORE_CLIENT_SECRET";
        var priorKey=Environment.GetEnvironmentVariable(keyVariable);var priorSecret=Environment.GetEnvironmentVariable(secretVariable);
        byte[]? key=null,credentials=null,pod=null;
        var plans=new List<(string Mode,CoreStorageConformance.Report Report,string Hash)>();
        var outcomes=new List<object>();
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
                // Admit both complete original cohorts before the first cleanup mutation.
                foreach(var mode in new[]{"async","sync"}){
                    var raw=await File.ReadAllBytesAsync(Path.Combine(root!.FullName,"TestResults","ProviderConformanceCurrent","SOLIDOASIS",mode,"report.json"));
                    var hash=Convert.ToHexString(SHA256.HashData(raw));
                    Assert.Equal(mode=="async"?"D23084C5207415895289C143FAF25341E7CEC6810DAAE7097AAFA3D2803BF3D6":"00D19842348903A161CBE2D97321160EE0BA9B858BCE384BB5896D3B6EDB8A15",hash);
                    var report=JsonSerializer.Deserialize<CoreStorageConformance.Report>(raw)!;
                    Assert.Equal(18,report.Passed.Count);Assert.Equal("holon-delete-"+report.ChildId.ToString("N"),report.CurrentCase);
                    var intent=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(original,"core-"+mode+".pending.json")));
                    using(intent){Assert.Equal(report.OwnerId,intent.RootElement.GetProperty("OwnerId").GetGuid());}
                    var marker="conformance-"+report.OwnerId.ToString("N");
                    var avatar=await provider.LoadAvatarAsync(report.OwnerId);var detail=await provider.LoadAvatarDetailAsync(report.OwnerId);
                    Assert.False(avatar.IsError);Assert.False(detail.IsError);
                    Assert.Equal(marker,avatar.Result.Username);Assert.Equal(marker,detail.Result.Username);
                    var child=await provider.LoadHolonAsync(report.ChildId,loadChildren:false);
                    Assert.True(child.IsError);Assert.Null(child.Result);
                    Assert.Equal(System.Net.HttpStatusCode.NotFound,Assert.IsType<HttpRequestException>(child.Exception).StatusCode);
                    foreach(var id in new[]{report.RootId,report.ImportId}.Concat(report.BatchIds)){
                        var row=await provider.LoadHolonAsync(id,loadChildren:false);Assert.False(row.IsError);
                        Assert.Equal(report.OwnerId,row.Result.CreatedByAvatarId);
                        Assert.Equal(marker+(id==report.RootId?"-updated":id==report.ImportId?"-import":"-batch"),row.Result.Name);
                    }
                    plans.Add((mode,report,hash));
                }
                async Task Checkpoint(bool complete){
                    await using(var file=new FileStream(pending+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                        await JsonSerializer.SerializeAsync(file,new{complete,scope="Exact two independently verified owned storage cohorts; deleted children never replayed",
                            intents=plans.Select(x=>new{x.Mode,x.Hash,x.Report.OwnerId,x.Report.RootId,x.Report.ChildId,x.Report.ImportId,x.Report.BatchIds}),outcomes});file.Flush(true);
                    }
                    File.Move(pending+".next",pending,true);
                }
                await Checkpoint(false);
                foreach(var plan in plans){
                    var r=plan.Report;
                    foreach(var id in new[]{r.RootId,r.ImportId}.Concat(r.BatchIds)){
                        var removed=await provider.DeleteHolonAsync(id);Assert.False(removed.IsError);Assert.True(removed.IsDeleted);Assert.Equal(1,removed.DeletedCount);
                        outcomes.Add(new{plan.Mode,kind="holon",id,deleted=true});await Checkpoint(false);
                    }
                    var avatar=await provider.DeleteAvatarAsync(r.OwnerId,softDelete:false);Assert.False(avatar.IsError);Assert.True(avatar.Result);
                    outcomes.Add(new{plan.Mode,kind="avatar-and-detail",id=r.OwnerId,deleted=true});await Checkpoint(false);
                    foreach(var id in new[]{r.RootId,r.ChildId,r.ImportId}.Concat(r.BatchIds)){
                        var absent=await provider.LoadHolonAsync(id,loadChildren:false);Assert.True(absent.IsError);Assert.Null(absent.Result);
                        Assert.Equal(System.Net.HttpStatusCode.NotFound,Assert.IsType<HttpRequestException>(absent.Exception).StatusCode);
                    }
                    Assert.Null((await provider.LoadAvatarAsync(r.OwnerId)).Result);Assert.Null((await provider.LoadAvatarDetailAsync(r.OwnerId)).Result);
                }
                await Checkpoint(true);File.Move(pending,closed);
            }
        }finally{
            Environment.SetEnvironmentVariable(keyVariable,priorKey);Environment.SetEnvironmentVariable(secretVariable,priorSecret);
            if(key!=null)CryptographicOperations.ZeroMemory(key);if(credentials!=null)CryptographicOperations.ZeroMemory(credentials);if(pod!=null)CryptographicOperations.ZeroMemory(pod);
        }
    }
}
