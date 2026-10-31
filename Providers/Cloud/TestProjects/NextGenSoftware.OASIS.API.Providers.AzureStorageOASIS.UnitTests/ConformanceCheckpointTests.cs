using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class ConformanceCheckpointTests
{
    private static string PathFor(string scenario)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var work=Path.Combine(root!.FullName,"TestResults","AzureCurrent","CheckpointControls",scenario);
        Directory.CreateDirectory(work);var path=Path.Combine(work,"report.json");
        Assert.IsFalse(File.Exists(path)||File.Exists(path+".next"),"Preserve previous failed controls before reuse.");return path;
    }

    [TestMethod]
    public async Task SuccessfulPublicationsReplaceThePreviousSnapshot()
    {
        var path=PathFor("success");var writer=new AtomicConformanceCheckpoint<string>(path);
        await writer.WriteAsync("first");await writer.WriteAsync("second");
        Assert.AreEqual("\"second\"",await File.ReadAllTextAsync(path));Assert.IsFalse(File.Exists(path+".next"));File.Delete(path);
    }

    [TestMethod]
    public async Task LockedTargetPreservesOriginalFailureAndPendingBytes()
    {
        var path=PathFor("locked-target");await File.WriteAllTextAsync(path,"original");
        var writer=new AtomicConformanceCheckpoint<string>(path);
        using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
            var first=await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(()=>writer.WriteAsync("pending"));
            var bytes=await File.ReadAllBytesAsync(path+".next");
            var second=await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(()=>writer.WriteAsync("must-not-overwrite"));
            Assert.AreSame(first,second);CollectionAssert.AreEqual(bytes,await File.ReadAllBytesAsync(path+".next"));
            Assert.AreEqual("original",await File.ReadAllTextAsync(path));
        }
        File.Delete(path+".next");File.Delete(path);
    }
}
