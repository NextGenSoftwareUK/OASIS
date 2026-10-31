using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeJoinCompilerCapturesSourcesAndMatchesFrozenValues(bool outer)=>WithNativeGraph(async fixture=>
    {
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Description=="L").OrderBy(h=>h.Name);
        var right=CosmicQuery<Holon>.Create().Where(h=>h.Description=="R");
        var selected=(outer?left.LeftJoin(right,h=>h.Name,h=>h.Name):left.Join(right,h=>h.Name,h=>h.Name))
            .Select((l,r)=>new{l.Name,Present=r!=null}).Result;
        var command=Provider.CompileCqlJoin(selected,new CosmicJoinExecutionLimits(10,10,100,100,10000,10000,10000));
        using var session=fixture.Driver.AsyncSession(config=>config.WithDefaultAccessMode(AccessMode.Read));
        var records=await(await session.RunAsync(command.Text,command.Parameters)).ToListAsync();
        Assert.AreEqual(2,records.Count(row=>row["kind"].As<long>()==0));
        Assert.AreEqual(2,records.Count(row=>row["kind"].As<long>()==1));
        var pairs=records.Where(row=>row["kind"].As<long>()==2).ToArray();
        Assert.AreEqual(outer?3:2,pairs.Length);
        Assert.AreEqual(outer?1:0,pairs.Count(row=>row["rp"].As<long>()==-1));
        Assert.IsTrue(pairs.All(row=>row["nativeCount"].As<long>()==pairs.Length));
    },configure:root=>
    {
        root.Name="coffee";root.Description="L";
        foreach(var child in root.Children){child.Name="coffee";child.Description="R";}
        var other=root.Children.First().Children.Single();other.Name="tea";other.Description="L";
    });

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeJoinCompilerRetainsProofsWhenEitherSourceIsEmpty(bool leftEmpty)=>WithNativeGraph(async fixture=>
    {
        var id=fixture.Root.Id;
        var source=CosmicQuery<Holon>.Create().Where(h=>h.Id==id);
        var empty=CosmicQuery<Holon>.Create().Where(h=>h.Name=="absent owned join source");
        var selected=(leftEmpty?empty.LeftJoin(source,h=>h.Name,h=>h.Name):source.LeftJoin(empty,h=>h.Name,h=>h.Name))
            .Select((l,r)=>l.Name).Result;
        var command=Provider.CompileCqlJoin(selected,new CosmicJoinExecutionLimits(10,10,100,100,10000,10000,10000));
        using var session=fixture.Driver.AsyncSession(config=>config.WithDefaultAccessMode(AccessMode.Read));
        var records=await(await session.RunAsync(command.Text,command.Parameters)).ToListAsync();
        Assert.AreEqual(leftEmpty?0:1,records.Count(row=>row["kind"].As<long>()==0));
        Assert.AreEqual(leftEmpty?1:0,records.Count(row=>row["kind"].As<long>()==1));
        Assert.AreEqual(leftEmpty?0:1,records.Count(row=>row["kind"].As<long>()==2));
    },width:0);
}
