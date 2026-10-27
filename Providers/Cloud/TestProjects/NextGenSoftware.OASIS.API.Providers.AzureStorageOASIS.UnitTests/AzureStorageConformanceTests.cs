using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class AzureStorageConformanceTests
{
    [DataTestMethod,DataRow(false),DataRow(true)]
    public async Task ShippingBlobFactoryRunsSharedStorageSuite(bool synchronous)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_AZURE_STORAGE_CONFORMANCE"));
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var mode=synchronous?"sync":"async";
        var work=Path.Combine(root!.FullName,"TestResults","ProviderConformanceCurrent","AzureStorageOASIS",mode);
        Directory.CreateDirectory(work);var reportPath=Path.Combine(work,"report.json");
        Assert.IsFalse(File.Exists(reportPath)||File.Exists(reportPath+".next"),"Preserve previous reports before a new run.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var containers=new[]{"oasis-conformance-holons-"+mode,"oasis-conformance-avatars-"+mode,"oasis-conformance-details-"+mode};
        var options=new BlobClientOptions();options.Retry.MaxRetries=0;
        // Fixed built-in emulator account and loopback endpoint; never uses hosted credentials.
        var native=new BlobServiceClient("UseDevelopmentStorage=true",options);
        Assert.AreEqual("127.0.0.1",native.Uri.Host);Assert.AreEqual(10000,native.Uri.Port);
        async Task AssertEmptyContainers()
        {
            foreach(var name in containers){
                var container=native.GetBlobContainerClient(name);
                if((await container.ExistsAsync()).Value)
                    await foreach(var row in container.GetBlobsAsync())Assert.Fail("Preserve existing fixture blobs; no adoption or prefix cleanup.");
            }
        }
        await AssertEmptyContainers();
        var settings=new AzureStorageOASISProviderSettings {
            ConnectionString="UseDevelopmentStorage=true",HolonContainer=containers[0],AvatarContainer=containers[1],
            AvatarDetailContainer=containers[2],IsVersionControlEnabled=false};
        var provider=CloudProviderFactory.CreateAzureStorage(settings,null!);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using(var file=new FileStream(reportPath+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(file,report);file.Flush(true);
            }
            File.Move(reportPath+".next",reportPath,overwrite:true);
        }
        var result=await CoreStorageConformance.RunAsync(provider,"AzureStorageOASIS","native",synchronous,Checkpoint,expectedDefaultDeletion:"absent");
        Assert.IsTrue(result.Succeeded,result.CurrentCase+": "+result.FailureType+": "+result.FailureContract);
        Assert.IsTrue(result.FixtureCleanupComplete);
        await AssertEmptyContainers();
    }
}
