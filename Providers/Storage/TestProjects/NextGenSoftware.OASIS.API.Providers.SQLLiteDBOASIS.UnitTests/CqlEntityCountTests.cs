using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

public sealed partial class CqlEntityCompositionOwnerTests
{
    public sealed class UnconstructibleCountRow : Holon
    {
        public UnconstructibleCountRow() => throw new InvalidOperationException("Count must not construct an entity.");
    }

    [TestMethod]
    public async Task NativeEntityCountPreservesAllSourceAndStageWindows()
    {
        var settings="Data Source=cosmic-entity-owner;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper=new SqliteConnection(settings); await keeper.OpenAsync();
        using var provider=new SQLLiteDBOASIS(settings);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        foreach(var name in new[] { "C","B","A" })
            Assert.IsFalse((await provider.SaveHolonAsync(new Holon { Id=Guid.NewGuid(),Name=name },saveChildren:false)).IsError);
        var source=CosmicQuery<Holon>.Create().OrderByDescending(row=>row.Name).Result.Take(2).Result;
        var absent=await provider.ExecuteCosmicEntityCountAsync(Plan(source.Compose().Result.Where(row=>row.Name=="A").Result));
        Assert.IsFalse(absent.IsError,absent.Message+absent.Exception); Assert.IsTrue(absent.IsLoaded); Assert.AreEqual(0L,absent.Result);
        var query=source.Compose().Result.OrderBy(row=>row.Name).Result.Skip(1).Result.Take(1).Result;
        var count=await provider.ExecuteCosmicEntityCountAsync(Plan(query));
        Assert.IsFalse(count.IsError,count.Message+count.Exception); Assert.AreEqual(1L,count.Result);
        var hidden=query.Take(0).Result;
        foreach(var options in new[] {new CosmicEntityCompositionOptions(maximumInputRows:1),
            new CosmicEntityCompositionOptions(maximumInputPayloadBytes:1),new CosmicEntityCompositionOptions(maximumStageRows:1)})
        {
            var rejected=await provider.ExecuteCosmicEntityCountAsync(Plan(hidden,options));
            Assert.AreEqual("CQL_QUERY_LIMIT",rejected.ErrorCode,rejected.Message+rejected.Exception); Assert.IsFalse(rejected.IsLoaded);
        }
        using var cancellation=new CancellationTokenSource(); cancellation.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED",(await provider.ExecuteCosmicEntityCountAsync(Plan(query),cancellation.Token)).ErrorCode);
    }

    [TestMethod]
    public async Task V2NativeCountDoesNotHydrateEntitiesOrExpandFinalGraphs()
    {
        var settings="Data Source=cosmic-entity-owner;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper=new SqliteConnection(settings); await keeper.OpenAsync();
        using var provider=new SQLLiteDBOASIS(settings);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var root=new Holon { Id=Guid.NewGuid(),Name="coffee" };
        root.Children.Add(new Holon { Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="child" });
        Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);
        var id=root.Id;
        var source=CosmicQuery<UnconstructibleCountRow>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result)
            .Where(row=>row.Id==id).WithChildren(CosmicQueryGraphOptions.Create(maximumNodes:1).Result).Take(1).Result;
        var query=source.Compose().Result.Where(row=>row.Name=="coffee").Result;
        var plan=CosmicEntityCompositionPlanner.Plan(query,ProviderType.SQLLiteDBOASIS,
            new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities),provider.EntityCompositionCapabilities);
        Assert.IsFalse(plan.IsError,plan.Message);
        var count=await Manager(provider).ExecuteEntityCountQueryAsync(plan.Result,new[] { provider });
        Assert.IsFalse(count.IsError,count.Message+count.Exception); Assert.AreEqual(1L,count.Result.Providers.Single().Value);
    }
}
