using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class SqliteStorageConformanceTests
{
    [DataTestMethod,DataRow(false),DataRow(true)]
    public async Task ShippingSqliteRunsSharedStorageSuite(bool synchronous)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_SQLITE_STORAGE_CONFORMANCE"));
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var work=Path.Combine(root!.FullName,"TestResults","ProviderConformanceCurrent","SQLLiteDBOASIS",synchronous?"sync":"async");
        Directory.CreateDirectory(work);var path=Path.Combine(work,"storage.sqlite");var report=Path.Combine(work,"report.json");
        Assert.IsFalse(File.Exists(path)||File.Exists(path+"-wal")||File.Exists(path+"-shm")||File.Exists(report)||File.Exists(report+".next"),"Preserve old database/checkpoints; never initialize over existing records.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var connection=new SqliteConnectionStringBuilder{DataSource=path,Pooling=false}.ToString();
        using var provider=new SQLLiteDBOASIS(connection);
        var checkpoint=new AtomicConformanceCheckpoint<CoreStorageConformance.Report>(report);
        var result=await CoreStorageConformance.RunAsync(provider,"SQLLiteDBOASIS","native",synchronous,checkpoint.WriteAsync);
        Assert.IsTrue(result.Succeeded,result.CurrentCase+": "+result.FailureType+": "+result.FailureContract);
        Assert.IsTrue(result.FixtureCleanupComplete);
    }
}
