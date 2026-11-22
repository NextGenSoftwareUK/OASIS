using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task BooleanWhereUsesNativeRowsCountProjectionAndActorVisibility(string providerName)
        => RunGroupedDistinctNativeFixture(providerName, false, whereJourney: true);

    private static async Task<int> VerifyWhereJourney(IOASISStorageProvider provider,
        List<Guid> ownedIds, Dictionary<string,long> timings)
    {
        var actor = Guid.NewGuid(); var foreign = Guid.NewGuid();
        var adapter = (ICosmicQueryStorageProvider)provider;
        var scalar = (ICosmicQueryScalarStorageProvider)provider;
        var projection = (ICosmicQueryProjectionStorageProvider)provider;
        var authority = adapter.QueryProviderType;
        Holon New(string name, Guid owner, Guid parent = default)
        {
            var row = new Holon { Id = Guid.NewGuid(), Name = name, CreatedByAvatarId = owner,
                IsPublic = false, IsActive = true, ParentHolonId = parent, IsNewHolon = true };
            ownedIds.Add(row.Id); return row;
        }
        var coffee = New("Coffee",actor); var tea = New("Tea",actor);
        var visibleChild = New("Leaf",actor,coffee.Id);
        var hiddenChild = New("Leaf",foreign,tea.Id);
        var foreignRoot = New("Tea",foreign);
        foreach (var row in new[] { coffee, tea, visibleChild, hiddenChild, foreignRoot })
        {
            var saved = await provider.SaveHolonAsync(row,saveChildren:false);
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            Assert.AreEqual(row.Id,saved.Result.Id);
        }
        CqlWhereV1Predicate Name(string value) => new() { Kind="Filter", Filter=new() {
            Field="Name", Scalar=CqlV1Scalar.String, Operator=CqlV1Operator.Equal,
            Value=JsonSerializer.SerializeToElement(value) } };
        CqlWhereV1Predicate Match(string name,bool absent) => new() { Kind="And",Terms=new[] {
            Name(name), absent ? new CqlWhereV1Predicate { Kind="Not", Terms=new[] {
                new CqlWhereV1Predicate {Kind="Child",Match=Name("Leaf")} } }
                : new CqlWhereV1Predicate {Kind="Child",Match=Name("Leaf")}
        }};
        var tree = new CqlWhereV1Predicate {Kind="Or",Terms=new[] {Match("Coffee",false),Match("Tea",true)}};
        var ids = new[] {coffee.Id,tea.Id,foreignRoot.Id};
        var filter = new CqlV1Filter { Field="Id",Scalar=CqlV1Scalar.Guid,Operator=CqlV1Operator.In,
            Value=JsonSerializer.SerializeToElement(ids) };
        var intent = CqlV1Compiler.Compile(new CqlV1Request {Provider=authority.ToString(),
            Filters=new[] {filter},Where=tree,OrderBy="Name",Skip=1,Take=1},actor,authority);
        var rowPlan = CosmicQueryPlanner.Plan(intent,authority,
            new[] {new CosmicQueryTarget(authority,adapter.QueryCapabilities)});
        Assert.IsFalse(rowPlan.IsError,rowPlan.Message);
        var timer=System.Diagnostics.Stopwatch.StartNew();
        var rows=await adapter.ExecuteCosmicQueryAsync(rowPlan.Result,maximumResults:10);
        timings.Add("native-paged-rows",timer.ElapsedMilliseconds);
        Assert.IsFalse(rows.IsError,rows.Message);Assert.IsTrue(rows.IsLoaded);
        Assert.AreEqual(tea.Id,rows.Result.Single().Id);

        var all=CqlV1Compiler.CompileScalarSource(new CqlScalarV1Request {Provider=authority.ToString(),
            Filters=new[] {filter},Where=tree,OrderBy="Id",Operation="LongCount"},actor,authority);
        var countPlan=CosmicScalarPlanner.Plan(all.LongCount().Result,authority,
            new[] {new CosmicScalarTarget(authority,adapter.QueryCapabilities,scalar.ScalarCapabilities)});
        Assert.IsFalse(countPlan.IsError,countPlan.Message);
        timer.Restart();var count=await scalar.ExecuteCosmicScalarAsync(countPlan.Result);
        timings.Add("native-count",timer.ElapsedMilliseconds);
        Assert.IsFalse(count.IsError,count.Message);Assert.AreEqual(2L,count.Result);

        var selected=CosmicProjectionPlanner.Plan(all.Select(h=>h.Name).Result,authority,
            new[] {new CosmicProjectionTarget(authority,adapter.QueryCapabilities,projection.ProjectionCapabilities)});
        Assert.IsFalse(selected.IsError,selected.Message);
        timer.Restart();var names=await projection.ExecuteCosmicProjectionAsync(selected.Result,maximumResults:10);
        timings.Add("native-projection",timer.ElapsedMilliseconds);
        Assert.IsFalse(names.IsError,names.Message);
        Assert.IsTrue(names.Result.All(row => row.Values.Count == 1 && row.Values[0] is string));
        CollectionAssert.AreEquivalent(new[] {"Coffee","Tea"},names.Result.Select(row => (string)row.Values[0]).ToArray());

        var other=CqlV1Compiler.Compile(new CqlV1Request {Provider=authority.ToString(),Where=tree,
            Filters=new[] {new CqlV1Filter {Field="Id",Scalar=CqlV1Scalar.Guid,Operator=CqlV1Operator.In,
                Value=JsonSerializer.SerializeToElement(new[] {coffee.Id,tea.Id})}},OrderBy="Id",Take=10},foreign,authority);
        var otherPlan=CosmicQueryPlanner.Plan(other,authority,new[] {new CosmicQueryTarget(authority,adapter.QueryCapabilities)});
        Assert.IsFalse(otherPlan.IsError,otherPlan.Message);
        timer.Restart();var denied=await adapter.ExecuteCosmicQueryAsync(otherPlan.Result,maximumResults:10);
        timings.Add("foreign-actor-isolation",timer.ElapsedMilliseconds);
        Assert.IsFalse(denied.IsError,denied.Message);Assert.AreEqual(0,denied.Result.Count);
        return 4;
    }
}
