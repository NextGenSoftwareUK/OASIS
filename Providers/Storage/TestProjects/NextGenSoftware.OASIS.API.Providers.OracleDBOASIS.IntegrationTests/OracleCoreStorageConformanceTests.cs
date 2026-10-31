using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Provider=NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.OracleDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class OracleCoreStorageConformanceTests
{
    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task CoreStorageSuiteAgainstRetainedOracle(bool sync)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING");
        Assert.IsFalse(string.IsNullOrWhiteSpace(connection),"Use the existing retained Oracle runner and protected secret.");
        var settings=new OracleConnectionStringBuilder(connection);
        Assert.AreEqual("127.0.0.1:11521/FREEPDB1",settings.DataSource);
        Assert.AreEqual("SYSTEM",settings.UserID);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var work=Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent","OracleDBOASIS",sync?"sync":"async");
        Directory.CreateDirectory(work);var reportPath=Path.Combine(work,"report.json");
        Assert.IsFalse(File.Exists(reportPath)||File.Exists(reportPath+".next"),"Preserve original Oracle conformance fixture before rerun.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using(var file=new FileStream(reportPath+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {await JsonSerializer.SerializeAsync(file,report);file.Flush(true);}
            File.Move(reportPath+".next",reportPath,overwrite:true);
        }
        var result=await CoreStorageConformance.RunAsync(new Provider(connection!),"OracleDBOASIS","native",sync,Checkpoint);
        Assert.IsTrue(result.Succeeded,result.CurrentCase+": "+result.FailureType);
    }
}
