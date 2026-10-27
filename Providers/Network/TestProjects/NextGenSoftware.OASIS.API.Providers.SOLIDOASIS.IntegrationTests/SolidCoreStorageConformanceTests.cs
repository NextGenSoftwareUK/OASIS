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
        Assert.False(File.Exists(path)||File.Exists(path+".next"),"Preserve original conformance evidence before another native run.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
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
                var original=Path.Combine(root.FullName,"TestResults","SolidCurrent");
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
                if(!new Uri("http://localhost:13000/oasis-auth-local/").IsBaseOf(new Uri(originalPod.RootElement.GetProperty("webId").GetString()!)))
                    throw new InvalidOperationException("Retained identity does not match the fixed private test Pod.");
                Environment.SetEnvironmentVariable(keyVariable,Convert.ToBase64String(key));
                Environment.SetEnvironmentVariable(secretVariable,client.RootElement.GetProperty("secret").GetString());
                provider=SolidProviderFactory.Create(new SolidOASISProviderSettings{
                    PodRoot="http://localhost:13000/oasis-auth-local/",AuthenticationMode="DpopClientCredentials",
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
            }
        }finally{
            Environment.SetEnvironmentVariable(keyVariable,priorKey);Environment.SetEnvironmentVariable(secretVariable,priorSecret);
            if(key!=null)CryptographicOperations.ZeroMemory(key);
            if(credentials!=null)CryptographicOperations.ZeroMemory(credentials);
            if(pod!=null)CryptographicOperations.ZeroMemory(pod);
        }
    }
}
