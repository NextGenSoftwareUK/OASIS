using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass,DoNotParallelize]
public class CoreStorageConformanceTests
{
    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task CommonInterfaceRunsAgainstTheExistingModelBackend(bool sync)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var report=await CoreStorageConformance.RunAsync(provider,"KeyValueStorageProviderBase","controlled",sync,_=>Task.CompletedTask);
        Assert.IsTrue(report.Succeeded,report.CurrentCase+": "+report.FailureType);
        Assert.IsTrue(report.Passed.Count>=25);
        Assert.AreEqual(0,backend.Items.Count,"Healthy suite must remove its owned records.");
    }

    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task SameInterfaceSuiteRunsAgainstNativeLocalFile(bool sync)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var work=Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent","LocalFileOASIS",sync?"sync":"async");
        var reportPath=Path.Combine(work,"report.json");var data=Path.Combine(work,"data");
        Assert.IsFalse(File.Exists(reportPath)||File.Exists(reportPath+".next"),"Review previous conformance evidence before rerun.");
        Directory.CreateDirectory(work);Directory.CreateDirectory(data);
        Assert.IsFalse(Directory.EnumerateFiles(data,"*",SearchOption.AllDirectories).Any(),"Never adopt existing native test records.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var provider=new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:data);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            var next=reportPath+".next";
            await using(var file=new FileStream(next,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {await JsonSerializer.SerializeAsync(file,report);file.Flush(true);}
            File.Move(next,reportPath,overwrite:true);
        }
        var result=await CoreStorageConformance.RunAsync(provider,"LocalFileOASIS","native",sync,Checkpoint);
        Assert.IsTrue(result.Succeeded,result.CurrentCase+": "+result.FailureType);
        Assert.IsFalse(Directory.EnumerateFiles(data,"*",SearchOption.AllDirectories).Any(),"Successful suite leaves no owned payload files.");
    }

}
