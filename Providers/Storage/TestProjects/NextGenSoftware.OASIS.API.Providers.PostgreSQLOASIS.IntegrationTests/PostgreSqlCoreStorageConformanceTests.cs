using System.Text.Json;
using Npgsql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Provider=NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.PostgreSQLOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class PostgreSqlCoreStorageConformanceTests
{
    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task CoreStorageSuiteAgainstRetainedPostgreSql(bool sync)
    {
        var connection=Environment.GetEnvironmentVariable("POSTGRESQLOASIS_CONNECTIONSTRING");
        Assert.IsFalse(string.IsNullOrWhiteSpace(connection),"Use the existing retained PostgreSQL runner and protected secret.");
        var settings=new NpgsqlConnectionStringBuilder(connection);
        Assert.AreEqual("127.0.0.1",settings.Host);Assert.AreEqual(15432,settings.Port);
        Assert.AreEqual("oasis_provider_test",settings.Database);Assert.AreEqual("oasis_test",settings.Username);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var work=Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent","PostgreSQLOASIS",sync?"sync":"async");
        Directory.CreateDirectory(work);var reportPath=Path.Combine(work,"report.json");
        Assert.IsFalse(File.Exists(reportPath)||File.Exists(reportPath+".next"),"Preserve original SQL conformance fixture before rerun.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using(var file=new FileStream(reportPath+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {await JsonSerializer.SerializeAsync(file,report);file.Flush(true);}
            File.Move(reportPath+".next",reportPath,overwrite:true);
        }
        var result=await CoreStorageConformance.RunAsync(new Provider(connection!),"PostgreSQLOASIS","native",sync,Checkpoint);
        Assert.IsTrue(result.Succeeded,result.CurrentCase+": "+result.FailureType);
    }
    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task PreservedDetailReadAcknowledgement(bool sync)
    {
        var connection=Environment.GetEnvironmentVariable("POSTGRESQLOASIS_CONNECTIONSTRING");
        Assert.IsFalse(string.IsNullOrWhiteSpace(connection));
        var settings=new NpgsqlConnectionStringBuilder(connection);
        Assert.AreEqual("127.0.0.1",settings.Host);Assert.AreEqual(15432,settings.Port);
        Assert.AreEqual("oasis_provider_test",settings.Database);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var reportPath=Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent","PostgreSQLOASIS",sync?"sync":"async","report.json");
        var report=JsonSerializer.Deserialize<CoreStorageConformance.Report>(await File.ReadAllTextAsync(reportPath));
        Assert.IsNotNull(report);Assert.AreEqual("PostgreSQLOASIS",report.Provider);
        Assert.AreEqual("native",report.Mode);Assert.AreEqual(sync,report.Synchronous);
        Assert.AreEqual("detail-id",report.CurrentCase);Assert.IsFalse(report.Succeeded);
        var provider=new Provider(connection!);
        // Read the original known-committed fixture; no save/replay/cleanup or report rewrite.
        var result=sync?provider.LoadAvatarDetail(report.OwnerId):await provider.LoadAvatarDetailAsync(report.OwnerId);
        Assert.IsFalse(result.IsError);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
        Assert.IsNotNull(result.Result);Assert.AreEqual(report.OwnerId,result.Result.Id);
        Assert.AreEqual(17,result.Result.Karma);Assert.AreEqual(31L,Convert.ToInt64(result.Result.XP));
        Assert.AreEqual("conformance-"+report.OwnerId.ToString("N"),result.Result.Username);
    }

}
