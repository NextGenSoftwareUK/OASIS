using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

public sealed partial class CqlEntityCompositionOwnerTests
{
    private static CosmicProjectionCompositionPlan<T,TResult> SelectedEntityPlan<T,TResult>(SQLLiteDBOASIS provider,
        CosmicEntityProjectionQuery<T,TResult> query,CosmicEntityCompositionOptions? options=null) where T:IHolon
    {
        var plan=CosmicProjectionCompositionPlanner.PlanEntity(query,ProviderType.SQLLiteDBOASIS,
            new CosmicProjectionCompositionTarget(ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities,provider.ProjectionCapabilities,provider.CompositionCapabilities),
            provider.EntityCompositionCapabilities,options);
        Assert.IsFalse(plan.IsError,plan.Message);return plan.Result;
    }

    [TestMethod]
    public async Task NativeSelectedStagesFollowTheEntityPageWithoutSourceConstructionOrPrecisionLoss()
    {
        var settings="Data Source=cosmic-entity-owner;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper=new SqliteConnection(settings);await keeper.OpenAsync();
        using var provider=new SQLLiteDBOASIS(settings);Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        foreach(var name in new[]{"C","B","A"})
            Assert.IsFalse((await provider.SaveHolonAsync(new Holon{Id=Guid.NewGuid(),Name=name,
                MetaData=new(){["precision"]=9007199254740993L}},saveChildren:false)).IsError);
        var source=CosmicQuery<UnconstructibleCountRow>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result)
            .OrderBy(row=>row.Name).Take(3).Result.Compose().Result.Reverse().Result.Take(2).Result;
        var selection=source.Select(row=>new StageProjectionDto{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"precision")}).Result;
        var query=selection.Where(row=>row.Precision>9007199254740992L).Result.OrderBy(row=>row.Name).Result.Take(1).Result;
        StageProjectionDto.Constructors=0;
        var read=await Manager(provider).ExecuteProjectionCompositionQueryAsync(SelectedEntityPlan(provider,query),new[]{provider});
        Assert.IsFalse(read.IsError,read.Message+read.Exception);
        var value=read.Result.Providers.Single().Rows.Single();
        Assert.AreEqual("B",value.Name);Assert.AreEqual(9007199254740993L,value.Precision);Assert.AreEqual(1,StageProjectionDto.Constructors);
        var reversed=selection.Reverse().Result.Skip(1).Result.Take(1).Result;
        var second=await Manager(provider).ExecuteProjectionCompositionQueryAsync(SelectedEntityPlan(provider,reversed),new[]{provider});
        Assert.IsFalse(second.IsError,second.Message+second.Exception);Assert.AreEqual("C",second.Result.Providers.Single().Rows.Single().Name);

        // The same two sequences are available through deferred standard LINQ.
        var root=CosmicLinq.Create(Manager(provider),provider,CosmicQuery<UnconstructibleCountRow>.Create(
            CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result)).Result;
        long threshold=9007199254740992L;
        var linq=root.OrderBy(row=>row.Name).Take(3).Reverse().Take(2)
            .Select(row=>new StageProjectionDto{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"precision")})
            .Where(row=>row.Precision>threshold).OrderBy(row=>row.Name).Take(1);
        var asyncRead=await linq.ToCosmicListAsync();
        Assert.IsFalse(asyncRead.IsError,asyncRead.Message+asyncRead.Exception);
        Assert.AreEqual("B",asyncRead.Result.Single().Name);
        Assert.AreEqual("B",linq.Reverse().ToArray().Single().Name);
        threshold=9007199254740993L;
        Assert.AreEqual(0,linq.ToArray().Length);
        var offsetPage=root.OrderBy(row=>row.Name).Skip(1).Take(2).Reverse()
            .Select(row=>row.Name).Take(1);
        Assert.AreEqual("C",offsetPage.ToArray().Single(),"Original offset must not be replayed after entity stages.");


    }

    [TestMethod]
    public async Task NativeSelectedWindowsCannotHideInvalidCellsOrEntityProtectedErrors()
    {
        var settings="Data Source=cosmic-entity-owner;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper=new SqliteConnection(settings);await keeper.OpenAsync();
        using var provider=new SQLLiteDBOASIS(settings);Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var root=new Holon{Id=Guid.NewGuid(),Name="root",MetaData=new(){["bad"]="text"}};
        root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="child"});
        root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="second"});
        Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);
        var id=root.Id;
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result)
            .Where(row=>row.Id==id).Take(1).Result.Compose().Result.Skip(0).Result;
        var invalid=source.Select(row=>new StageProjectionDto{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"bad")}).Result.Take(0).Result;
        StageProjectionDto.Constructors=0;
        var read=await Manager(provider).ExecuteProjectionCompositionQueryAsync(SelectedEntityPlan(provider,invalid),new[]{provider});
        Assert.AreEqual("CQL_DATA_INVALID",read.ErrorCode,read.Message+read.Exception);Assert.AreEqual(0,StageProjectionDto.Constructors);
        var tight=CosmicQueryTraversalOptions.Create(2,1).Result;
        var protectedQuery=source.Where(row=>row.Name=="root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight)).Result
            .Take(0).Result.Select(row=>row.Name).Result.Take(0).Result;
        var failed=await Manager(provider).ExecuteProjectionCompositionQueryAsync(SelectedEntityPlan(provider,protectedQuery),new[]{provider});
        Assert.AreEqual("CQL_QUERY_LIMIT",failed.ErrorCode,failed.Message+failed.Exception);
        var bounded=source.Take(0).Result.Select(row=>row.Name).Result.Take(0).Result;
        var budget=await Manager(provider).ExecuteProjectionCompositionQueryAsync(SelectedEntityPlan(provider,bounded,
            new CosmicEntityCompositionOptions(maximumInputRows:1,maximumStageRows:1)),new[]{provider});
        Assert.AreEqual("CQL_QUERY_LIMIT",budget.ErrorCode,budget.Message+budget.Exception);
    }
}
