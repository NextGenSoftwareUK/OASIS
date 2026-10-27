using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Providers.TestSupport;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class AzureStorageConformanceTests
{
    [TestMethod]
    public async Task PreserveExactInitialAvatarSaveFailures()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_AZURE_RECONCILE_AVATAR_SAVE"));
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);var repository=root!.FullName;
        using var lease=new FileStream(Path.Combine(repository,"TestResults","AzureCurrent","environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var registry=JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(repository,"Docs","WIP","HyperDrive-Providers","CONFORMANCE_RESULTS.json")))!;
        var entry=registry["results"]!.AsArray().Single(row=>row!["provider"]!.GetValue<string>()=="AzureStorageOASIS")!;
        var aggregatePath=Path.GetFullPath(Path.Combine(repository,entry["report"]!.GetValue<string>()));
        Assert.IsTrue(aggregatePath.StartsWith(repository+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase));
        var aggregateRaw=await File.ReadAllBytesAsync(aggregatePath);
        Assert.AreEqual(entry["sha256"]!.GetValue<string>(),Convert.ToHexString(SHA256.HashData(aggregateRaw)).ToLowerInvariant());
        var aggregate=JsonNode.Parse(aggregateRaw)!;Assert.AreEqual("core-storage-v2",aggregate["suite"]!.GetValue<string>());
        Assert.AreEqual(2,aggregate["runs"]!.AsArray().Count);
        var options=new BlobClientOptions();options.Retry.MaxRetries=0;
        var native=new BlobServiceClient("UseDevelopmentStorage=true",options);
        var evidence=Path.Combine(repository,"TestResults","HyperDrive-Providers","NativeEvidence");
        var receiptPath=Path.Combine(evidence,"azure-blob-initial-fixture-reconciliation.json");
        Assert.IsFalse(File.Exists(receiptPath),"Existing reconciliation requires inspection, never replay.");
        var plans=new List<(BlobClient Blob,ETag Etag,byte[] Payload,string PayloadPath,string Report,string Archive)>();
        foreach(var mode in new[]{"async","sync"}){
            var reportPath=Path.Combine(repository,"TestResults","ProviderConformanceCurrent","AzureStorageOASIS",mode,"report.json");
            var report=JsonNode.Parse(await File.ReadAllBytesAsync(reportPath))!;
            var expected=aggregate["runs"]!.AsArray().Single(row=>row!["Synchronous"]!.GetValue<bool>()==(mode=="sync"));
            Assert.IsTrue(JsonNode.DeepEquals(report,expected));
            Assert.AreEqual("AzureStorageOASIS",report["Provider"]!.GetValue<string>());Assert.AreEqual("native",report["Mode"]!.GetValue<string>());
            Assert.IsFalse(report["Succeeded"]!.GetValue<bool>());Assert.AreEqual("avatar-save",report["CurrentCase"]!.GetValue<string>());
            Assert.AreEqual("save acknowledgement/count",report["FailureContract"]!.GetValue<string>());
            Assert.AreEqual("[\"activate\"]",report["Passed"]!.ToJsonString());
            var id=report["OwnerId"]!.GetValue<string>();Assert.IsTrue(Guid.TryParse(id,out _));
            var names=new[]{"oasis-conformance-holons-"+mode,"oasis-conformance-avatars-"+mode,"oasis-conformance-details-"+mode};
            foreach(var name in names){
                var blobs=new List<string>();await foreach(var row in native.GetBlobContainerClient(name).GetBlobsAsync())blobs.Add(row.Name);
                CollectionAssert.AreEquivalent(name==names[1]?new[]{id}:Array.Empty<string>(),blobs);
            }
            var provider=CloudProviderFactory.CreateAzureStorage(new AzureStorageOASISProviderSettings{ConnectionString="UseDevelopmentStorage=true",HolonContainer=names[0],AvatarContainer=names[1],AvatarDetailContainer=names[2]},null!);
            var loaded=await provider.LoadAvatarAsync(Guid.Parse(id));
            Assert.IsFalse(loaded.IsError);Assert.AreEqual(Guid.Parse(id),loaded.Result.Id);
            Assert.AreEqual("conformance-"+id.Replace("-",""),loaded.Result.Username);
            Assert.AreEqual(loaded.Result.Username+"@example.invalid",loaded.Result.Email);
            var blob=native.GetBlobContainerClient(names[1]).GetBlobClient(id);var snapshot=(await blob.DownloadContentAsync()).Value;
            Assert.IsFalse(string.IsNullOrWhiteSpace(snapshot.Details.ETag.ToString()));
            var preserved=OasisJson.DeserializeStored<Avatar>(snapshot.Content.ToString());
            Assert.IsNotNull(preserved);Assert.AreEqual(Guid.Parse(id),preserved.Id);
            Assert.AreEqual(loaded.Result.Username,preserved.Username);Assert.AreEqual(loaded.Result.Email,preserved.Email);
            Assert.AreEqual(preserved.Username,preserved.FirstName);Assert.IsTrue(preserved.IsActive);
            var payloadPath=Path.Combine(evidence,"azure-blob-initial-"+mode+"-avatar.json");var archive=Path.Combine(Path.GetDirectoryName(reportPath)!,"report-initial-avatar-save.json");
            Assert.IsFalse(File.Exists(payloadPath)||File.Exists(archive)||File.Exists(reportPath+".next"));
            plans.Add((blob,snapshot.Details.ETag,snapshot.Content.ToArray(),payloadPath,reportPath,archive));
        }
        var receipt=new JsonObject{["state"]="prepared",["acknowledgedDeletes"]=new JsonArray(),["payloads"]=new JsonArray()};
        foreach(var plan in plans){
            await using(var file=new FileStream(plan.PayloadPath,FileMode.CreateNew,FileAccess.Write,FileShare.None)){await file.WriteAsync(plan.Payload);file.Flush(true);}
            receipt["payloads"]!.AsArray().Add(new JsonObject{["path"]=Path.GetRelativePath(repository,plan.PayloadPath),["sha256"]=Convert.ToHexString(SHA256.HashData(plan.Payload)),["etag"]=plan.Etag.ToString()});
        }
        async Task Checkpoint(){await File.WriteAllTextAsync(receiptPath,receipt.ToJsonString(new JsonSerializerOptions{WriteIndented=true}));}
        await Checkpoint();
        foreach(var plan in plans){
            var response=await plan.Blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=plan.Etag});
            Assert.IsTrue(response.Status>=200&&response.Status<300);Assert.IsFalse((await plan.Blob.ExistsAsync()).Value);
            receipt["acknowledgedDeletes"]!.AsArray().Add(plan.Blob.Name);await Checkpoint();
        }
        foreach(var plan in plans)File.Move(plan.Report,plan.Archive);
        receipt["state"]="exact synthetic avatars preserved and conditionally removed";await Checkpoint();
    }

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
