using System.Text.Json;
using System.Security.Cryptography;
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
        var provider=new Provider(connection!);
        var before=await Snapshot(connection!);
        // Shipping queries hide soft-deleted records: the public contract is interface absence.
        var result=await CoreStorageConformance.RunAsync(provider,"PostgreSQLOASIS","native",sync,Checkpoint,expectedDefaultDeletion:"absent");
        var after=await Snapshot(connection!);
        var preserved=before.All(pair=>after.TryGetValue(pair.Key,out var count)&&count>=pair.Value);
        await File.WriteAllTextAsync(Path.Combine(work,"existing-record-preservation.json"),JsonSerializer.Serialize(new {
            BeforeCount=before.Values.Sum(),AfterCount=after.Values.Sum(),Preserved=preserved,
            BeforeSha256=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",before.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value))))) }));
        if(!preserved){result.Succeeded=false;result.CurrentCase="existing-record-preservation";result.FailureType="System.InvalidOperationException";await Checkpoint(result);}
        Assert.IsTrue(preserved,"Every pre-existing public-table row must remain unchanged.");
        Assert.IsTrue(result.Succeeded,result.CurrentCase+": "+result.FailureType);
    }

    private static async Task<Dictionary<string,int>> Snapshot(string connection)
    {
        await using var db=new NpgsqlConnection(connection);await db.OpenAsync();
        await using var tables=new NpgsqlCommand("SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename",db);
        var names=new List<string>();
        await using(var reader=await tables.ExecuteReaderAsync())while(await reader.ReadAsync())names.Add(reader.GetString(0));
        var rows=new Dictionary<string,int>();
        using var quoting=new NpgsqlCommandBuilder();
        foreach(var table in names){
            await using var command=new NpgsqlCommand("SELECT row_to_json(t)::text FROM public."+quoting.QuoteIdentifier(table)+" t",db);
            await using var reader=await command.ExecuteReaderAsync();
            while(await reader.ReadAsync()){
                var key=table+":"+Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(reader.GetString(0))));
                rows[key]=rows.GetValueOrDefault(key)+1;
            }
        }
        return rows;
    }

    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task PreservedParentReadAcknowledgement(bool sync)
    {
        var connection=Environment.GetEnvironmentVariable("POSTGRESQLOASIS_CONNECTIONSTRING");
        var settings=new NpgsqlConnectionStringBuilder(connection);
        Assert.AreEqual("127.0.0.1",settings.Host);Assert.AreEqual(15432,settings.Port);
        Assert.AreEqual("oasis_provider_test",settings.Database);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var path=Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent","PostgreSQLOASIS",sync?"sync":"async","report.json");
        var report=JsonSerializer.Deserialize<CoreStorageConformance.Report>(await File.ReadAllTextAsync(path));
        Assert.IsNotNull(report);Assert.AreEqual("parent-query",report.CurrentCase);Assert.IsFalse(report.Succeeded);
        var before=await Snapshot(connection!);
        var provider=new Provider(connection!);
        var result=sync?provider.LoadHolonsForParent(report.RootId,loadChildren:false):await provider.LoadHolonsForParentAsync(report.RootId,loadChildren:false);
        Assert.IsFalse(result.IsError);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
        Assert.IsNotNull(result.Result);var child=result.Result.Single();
        Assert.AreEqual(report.ChildId,child.Id);Assert.AreEqual(report.OwnerId,child.CreatedByAvatarId);
        var after=await Snapshot(connection!);
        Assert.AreEqual(before.Count,after.Count);Assert.IsTrue(before.All(pair=>after.GetValueOrDefault(pair.Key)==pair.Value),"Read regression cannot mutate any original row.");
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
        var originalPath=Path.Combine(Path.GetDirectoryName(reportPath)!,"report-core-storage-v1.json");
        if(File.Exists(originalPath))reportPath=originalPath;
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
