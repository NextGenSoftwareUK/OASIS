using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class Neo4jCoreStorageConformanceTests
{
    private static object? Stable(object? value)
    {
        if(value==null)return null;
        if(value is IReadOnlyDictionary<string,object> map)return new SortedDictionary<string,object?>(map.ToDictionary(pair=>pair.Key,pair=>Stable(pair.Value)),StringComparer.Ordinal);
        if(value is IEnumerable sequence && value is not string)return sequence.Cast<object?>().Select(Stable).ToArray();
        return value.GetType().FullName+":"+Convert.ToString(value,CultureInfo.InvariantCulture);
    }
    private static string Hash(object value)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static async Task<Dictionary<string,string>> Snapshot(IDriver driver)
    {
        await using var session=driver.AsyncSession();var rows=new Dictionary<string,string>();
        var nodes=await (await session.RunAsync("MATCH (n) RETURN n")).ToListAsync(row=>row["n"].As<INode>());
        foreach(var node in nodes)rows.Add("node:"+node.Id,Hash(new{Labels=node.Labels.OrderBy(label=>label,StringComparer.Ordinal).ToArray(),Properties=Stable(node.Properties)}));
        var relationships=await (await session.RunAsync("MATCH ()-[r]->() RETURN r")).ToListAsync(row=>row["r"].As<IRelationship>());
        foreach(var edge in relationships)rows.Add("edge:"+edge.Id,Hash(new{edge.StartNodeId,edge.EndNodeId,edge.Type,Properties=Stable(edge.Properties)}));
        return rows;
    }

    [DataTestMethod,DataRow(false),DataRow(true),TestCategory("Neo4jCoreStorageConformance")]
    public async Task SharedStorageInterfacePreservesTheExistingGraph(bool sync)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var work=Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent","Neo4jOASIS",sync?"sync":"async");
        Directory.CreateDirectory(work);var reportPath=Path.Combine(work,"report.json");
        Assert.IsFalse(File.Exists(reportPath)||File.Exists(reportPath+".next"),"Preserve original fixture/report before any new write.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using(var file=new FileStream(reportPath+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {await JsonSerializer.SerializeAsync(file,report);file.Flush(true);}
            File.Move(reportPath+".next",reportPath,overwrite:true);
        }
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        var before=await Snapshot(driver);
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        var result=await CoreStorageConformance.RunAsync(provider,"Neo4jOASIS","native",sync,Checkpoint,expectedDefaultDeletion:"absent");
        var after=await Snapshot(driver);
        var preserved=before.All(pair=>after.TryGetValue(pair.Key,out var fingerprint)&&fingerprint==pair.Value);
        await File.WriteAllTextAsync(Path.Combine(work,"existing-graph-preservation.json"),JsonSerializer.Serialize(new{ExistingCount=before.Count,ExistingSha256=Hash(new SortedDictionary<string,string>(before,StringComparer.Ordinal)),Preserved=preserved,AfterCount=after.Count}));
        if(!preserved){result.Succeeded=false;result.CurrentCase="existing-graph-preservation";result.FailureType="ExistingGraphChanged";await Checkpoint(result);}
        Assert.IsTrue(preserved,"An original graph element changed; preserve all evidence, never reconcile unrelated records.");
        Assert.IsTrue(result.Succeeded,result.CurrentCase+": "+result.FailureType+" ["+result.FailureContract+"]");
    }
}
