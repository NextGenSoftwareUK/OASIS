using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static CosmicQuery<Holon> ProjectedAllSource(GraphFixture fixture)
    {
        var owner = fixture.Root.Id;
        var query = CosmicQuery<Holon>.Create().Where(row => row.CreatedByAvatarId == owner);
        Assert.IsFalse(query.IsError, query.Message); return query.Result;
    }
    private static CosmicProjectedAllPlan<Holon, TResult> ProjectedAllPlan<TResult>(CosmicProjectedAllQuery<Holon, TResult> query,
        CosmicProjectionCompositionOptions? options = null)
    {
        var plan = CosmicProjectedAllPlanner.Plan(query, ProviderType.Neo4jOASIS,
            new[] { new CosmicProjectionCompositionTarget(ProviderType.Neo4jOASIS, Provider.CqlCapabilities,
                Provider.CqlProjectionCapabilities, Provider.CqlCompositionCapabilities) }, options);
        Assert.IsFalse(plan.IsError, plan.Message); return plan.Result;
    }
    private static async Task<bool> ProjectedAllRead<TResult>(GraphFixture fixture, CosmicProjectedAllQuery<Holon, TResult> query)
    {
        var result = await GraphManager(fixture.Provider).ExecuteProjectedAllQueryAsync(ProjectedAllPlan(query), new[] { fixture.Provider });
        Assert.IsFalse(result.IsError, result.Message + result.Exception); Assert.IsTrue(result.IsLoaded);
        return result.Result.Providers.Single().Value;
    }
    private static object? AllStable(object? value)
    {
        if (value == null) return null;
        if (value is IReadOnlyDictionary<string, object> map)
            return new SortedDictionary<string, object?>(map.ToDictionary(pair => pair.Key, pair => AllStable(pair.Value)), StringComparer.Ordinal);
        if (value is IEnumerable sequence && value is not string) return sequence.Cast<object?>().Select(AllStable).ToArray();
        return value.GetType().FullName + ":" + Convert.ToString(value, CultureInfo.InvariantCulture);
    }
    private static async Task<string> AllFingerprint(IDriver driver)
    {
        using var session = driver.AsyncSession(); var rows = new SortedDictionary<string, string>(StringComparer.Ordinal);
        string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
        var nodes = await (await session.RunAsync("MATCH (n) RETURN n")).ToListAsync(row => row["n"].As<INode>());
        foreach (var node in nodes) rows.Add("node:" + node.Id, Hash(new { Labels=node.Labels.OrderBy(label=>label,StringComparer.Ordinal).ToArray(), Properties=AllStable(node.Properties) }));
        var edges = await (await session.RunAsync("MATCH ()-[r]->() RETURN r")).ToListAsync(row => row["r"].As<IRelationship>());
        foreach (var edge in edges) rows.Add("edge:" + edge.Id, Hash(new { edge.StartNodeId,edge.EndNodeId,edge.Type,Properties=AllStable(edge.Properties) }));
        return Hash(rows);
    }
    private static async Task WithProjectedAllGraph(Func<GraphFixture, Task> test, int width = 2, Action<Holon>? configure = null)
    {
        // Reuse the retained store: every query is ID-scoped, and existing nodes,
        // relationships, labels and properties must survive exact fixture cleanup.
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        var before = await AllFingerprint(driver);
        await WithNativeGraph(test, width: width, configure: root =>
        {
            configure?.Invoke(root);
            // Native provider saves retain the supplied version. Set test data
            // explicitly rather than relying on HolonManager's save preparation.
            var nodes = new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> { root };
            for (var index = 0; index < nodes.Count; index++)
            {
                nodes[index].Version = 1; nodes[index].CreatedByAvatarId = root.Id;
                nodes.AddRange(nodes[index].AllChildren);
            }
        });
        Assert.AreEqual(before, await AllFingerprint(driver), "Existing graph changed or owned fixture teardown is incomplete.");
    }

    [TestMethod]
    public Task NativeProjectedAllFindsALateCounterexampleBeyondThePublicPage()
        => WithProjectedAllGraph(async fixture =>
        {
            var selected = ProjectedAllSource(fixture).OrderBy(row => CosmicQueryMetadata.Value<int>(row,"ordinal"))
                .Select(row => CosmicQueryMetadata.Value<int>(row,"ordinal"));
            Assert.IsFalse(await ProjectedAllRead(fixture, selected.All(value => value < 149).Result));
            Assert.IsTrue(await ProjectedAllRead(fixture, selected.Take(149).All(value => value < 149).Result));
        }, width:149, configure:root => { root.MetaData["ordinal"]=0; var index=1; foreach(var child in root.Children)child.MetaData["ordinal"]=index++; });

    [TestMethod]
    public Task NativeProjectedAllRunsAfterEverySourceAndSelectedWindow()
        => WithProjectedAllGraph(async fixture =>
        {
            var selected = ProjectedAllSource(fixture).OrderBy(row=>row.Name).Skip(1).Take(3)
                .Select(row=>new {row.Name,row.Version}).Where(row=>row.Name!="B").OrderBy(row=>row.Name).Skip(1).Take(1);
            Assert.IsTrue(await ProjectedAllRead(fixture, selected.All(row=>row.Name=="D" && row.Version>0).Result));
            Assert.IsFalse(await ProjectedAllRead(fixture, selected.All(row=>row.Name=="C").Result));
        }, configure:root => { root.Name="A";var children=root.Children.ToArray();children[0].Name="B";children[1].Name="C";children[0].Children.Single().Name="D"; });

    [TestMethod]
    public Task NativeProjectedAllAcknowledgesEmptySourceAndSelectedTakeZero()
        => WithProjectedAllGraph(async fixture =>
        {
            var empty=Guid.Empty;
            Assert.IsTrue(await ProjectedAllRead(fixture,ProjectedAllSource(fixture).Where(row=>row.Id==empty).Select(row=>row.Name).All(value=>false).Result));
            Assert.IsTrue(await ProjectedAllRead(fixture,ProjectedAllSource(fixture).Select(row=>row.Name).Take(0).All(value=>false).Result));
        },width:0);

    [TestMethod]
    public Task NativeProjectedAllPreservesAllThirtyTwoSelectedStages()
        => WithProjectedAllGraph(async fixture =>
        {
            var selected=ProjectedAllSource(fixture).Select(row=>row.Version);
            for(var index=0;index<32;index++)selected=selected.Take(1);
            Assert.IsTrue(await ProjectedAllRead(fixture,selected.All(value=>value>0).Result));
        },width:0);

    [DataTestMethod,DataRow(false),DataRow(true)]
    public Task NativeProjectedAllCannotHideInvalidRequiredCellsWithEmptyWindows(bool malformed)
        => WithProjectedAllGraph(async fixture =>
        {
            var query=ProjectedAllSource(fixture).Select(row=>CosmicQueryMetadata.Value<int>(row,"required")).Take(0).All(value=>true).Result;
            var result=await GraphManager(fixture.Provider).ExecuteProjectedAllQueryAsync(ProjectedAllPlan(query),new[]{fixture.Provider});
            Assert.AreEqual("CQL_DATA_INVALID",result.ErrorCode);Assert.IsNull(result.Result);
        },width:0,configure:root=>{if(malformed)root.MetaData["required"]="wrong-domain";});

    [DataTestMethod,DataRow(3,100,false),DataRow(4,3,false),DataRow(4,4,true)]
    public Task NativeProjectedAllEnforcesCompleteInputAndTerminalWork(int input,int stage,bool succeeds)
        => WithProjectedAllGraph(async fixture =>
        {
            var query=ProjectedAllSource(fixture).Select(row=>row.Version).All(value=>value>0).Result;
            var result=await GraphManager(fixture.Provider).ExecuteProjectedAllQueryAsync(
                ProjectedAllPlan(query,new CosmicProjectionCompositionOptions(input,10000,stage,10000)),new[]{fixture.Provider});
            Assert.AreEqual(succeeds,!result.IsError,result.Message+result.Exception);
            if(succeeds)Assert.IsTrue(result.Result.Providers.Single().Value);
            else {Assert.AreEqual("CQL_QUERY_LIMIT",result.ErrorCode);Assert.IsNull(result.Result);}
        });

    [DataTestMethod,DataRow(0),DataRow(1),DataRow(2),DataRow(3),DataRow(4),DataRow(5)]
    public Task NativeProjectedAllHonorsEveryRemainingBudgetAndFreshReuse(int exhausted)
        => WithProjectedAllGraph(async fixture =>
        {
            var query=ProjectedAllSource(fixture).Select(row=>row.Name).All(value=>value!=null).Result;
            var limits=new[]{10,10000,100,10000,10,10000};limits[exhausted]=0;
            var result=await fixture.Provider.ExecuteCosmicProjectedAllAsync(ProjectedAllPlan(query),new CosmicProjectionCompositionExecutionLimits(
                limits[0],limits[1],limits[2],limits[3],limits[4],limits[5]));
            Assert.AreEqual("CQL_QUERY_LIMIT",result.ErrorCode,result.Message+result.Exception);Assert.IsNull(result.Result);
            Assert.IsTrue(await ProjectedAllRead(fixture,query));
        },width:0);

    public sealed class AllNeverConstructed
    {
        public AllNeverConstructed()=>throw new Exception("Projected All cannot construct a DTO.");
        public int Revision {get;set;}
    }
    [TestMethod]
    public Task NativeProjectedAllPreservesNullSemanticsAndDoesNotConstructDtos()
        => WithProjectedAllGraph(async fixture =>
        {
            var selected=ProjectedAllSource(fixture).Select(row=>CosmicQueryMetadata.Value<string>(row,"optional"));
            Assert.IsTrue(await ProjectedAllRead(fixture,selected.All(value=>value==null || value=="coffee").Result));
            Assert.IsFalse(await ProjectedAllRead(fixture,selected.All(value=>value!=null).Result));
            Assert.IsTrue(await ProjectedAllRead(fixture,ProjectedAllSource(fixture).Select(row=>new AllNeverConstructed{Revision=row.Version}).All(row=>row.Revision>0).Result));
        },width:0,configure:root=>root.MetaData["optional"]=null!);

    [TestMethod]
    public Task NativeProjectedAllPreCancellationCannotPublishAndAllowsReuse()
        => WithProjectedAllGraph(async fixture =>
        {
            var query=ProjectedAllSource(fixture).Select(row=>row.Name).All(value=>value!=null).Result;
            using var cancellation=new CancellationTokenSource();cancellation.Cancel();
            var result=await GraphManager(fixture.Provider).ExecuteProjectedAllQueryAsync(ProjectedAllPlan(query),new[]{fixture.Provider},cancellation.Token);
            Assert.AreEqual("CQL_QUERY_CANCELLED",result.ErrorCode);Assert.IsNull(result.Result);
            Assert.IsTrue(await ProjectedAllRead(fixture,query));
        },width:0);

    [TestMethod]
    public Task NativeProjectedAllSharedDecoderKeepsOrdinarySelectedCompositionWorking()
        => WithProjectedAllGraph(async fixture =>
        {
            var query=ProjectedAllSource(fixture).Select(row=>row.Name).Take(1).Result;
            var result=await GraphManager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(CompositionPlan(query),new[]{fixture.Provider});
            Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.AreEqual(1,result.Result.Providers.Single().Rows.Count);
        },width:0);
}
