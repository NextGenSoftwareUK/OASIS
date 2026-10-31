using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

public sealed partial class CqlEntityCompositionOwnerTests
{
    public sealed class StageProjectionDto
    {
        public static int Constructors;
        public StageProjectionDto(){Constructors++;}
        public string Name {get;set;}="";
        public long Precision {get;set;}
        public ulong Unsigned {get;set;}
        public decimal Amount {get;set;}
    }
    private static CosmicEntityProjectionPlan<T,TResult> ProjectionPlan<T,TResult>(SQLLiteDBOASIS provider,
        CosmicEntityProjectionQuery<T,TResult> query,CosmicEntityCompositionOptions? options=null) where T:IHolon
    {
        var plan=CosmicEntityProjectionPlanner.Plan(query,ProviderType.SQLLiteDBOASIS,
            new CosmicProjectionTarget(ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities,provider.ProjectionCapabilities),provider.EntityCompositionCapabilities,options);
        Assert.IsFalse(plan.IsError,plan.Message);return plan.Result;
    }
    [TestMethod]
    public async Task NativeProjectionFollowsSourceAndStageWindowsWithoutEntityConstructionOrPrecisionLoss()
    {
        var settings="Data Source=cosmic-entity-owner;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper=new SqliteConnection(settings);await keeper.OpenAsync();
        using var provider=new SQLLiteDBOASIS(settings);Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        const decimal amount=1.0000000000000000000000000001m;
        foreach(var name in new[]{"C","B","A"})
            Assert.IsFalse((await provider.SaveHolonAsync(new Holon {Id=Guid.NewGuid(),Name=name,
                MetaData=new(){["precision"]=9007199254740993L,["unsigned"]=ulong.MaxValue,["amount"]=amount}},saveChildren:false)).IsError);
        var source=CosmicQuery<UnconstructibleCountRow>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result)
            .OrderBy(row=>row.Name).Take(2).Result.Compose().Result.Reverse().Result.Take(1).Result;
        StageProjectionDto.Constructors=0;
        var selected=source.Select(row=>new StageProjectionDto {Name=row.Name,
            Precision=CosmicQueryMetadata.Value<long>(row,"precision"),Unsigned=CosmicQueryMetadata.Value<ulong>(row,"unsigned"),
            Amount=CosmicQueryMetadata.Value<decimal>(row,"amount")});
        Assert.IsFalse(selected.IsError,selected.Message);Assert.AreEqual(0,StageProjectionDto.Constructors);
        var read=await Manager(provider).ExecuteEntityProjectionQueryAsync(ProjectionPlan(provider,selected.Result),new[]{provider});
        Assert.IsFalse(read.IsError,read.Message+read.Exception);
        var result=read.Result.Providers.Single().Rows.Single();
        Assert.AreEqual("B",result.Name);Assert.AreEqual(9007199254740993L,result.Precision);
        Assert.AreEqual(ulong.MaxValue,result.Unsigned);Assert.AreEqual(amount,result.Amount);Assert.AreEqual(1,StageProjectionDto.Constructors);
        var missing=source.Where(row=>row.Name=="C").Result.Select(row=>row.Name).Result;
        var empty=await Manager(provider).ExecuteEntityProjectionQueryAsync(ProjectionPlan(provider,missing),new[]{provider});
        Assert.IsFalse(empty.IsError,empty.Message+empty.Exception);Assert.AreEqual(0,empty.Result.Providers.Single().Rows.Count);
    }
    [TestMethod]
    public async Task NativeProjectionKeepsProtectedErrorsBudgetsAndCellValidationBeforeDtos()
    {
        var settings="Data Source=cosmic-entity-owner;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper=new SqliteConnection(settings);await keeper.OpenAsync();
        using var provider=new SQLLiteDBOASIS(settings);Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var root=new Holon {Id=Guid.NewGuid(),Name="root",MetaData=new(){["bad"]="wrong domain"}};
        root.Children.Add(new Holon {Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="child"});
        root.Children.Add(new Holon {Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="second"});
        Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);var id=root.Id;
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result).Where(row=>row.Id==id).Take(1).Result.Compose().Result;
        StageProjectionDto.Constructors=0;
        var invalid=source.Skip(0).Result.Select(row=>new StageProjectionDto {Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"bad")}).Result;
        var rejected=await Manager(provider).ExecuteEntityProjectionQueryAsync(ProjectionPlan(provider,invalid),new[]{provider});
        Assert.AreEqual("CQL_DATA_INVALID",rejected.ErrorCode,rejected.Message+rejected.Exception);Assert.AreEqual(0,StageProjectionDto.Constructors);
        var tight=CosmicQueryTraversalOptions.Create(2,1).Result;
        var protectedQuery=source.Where(row=>row.Name=="root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight)).Result
            .Take(0).Result.Select(row=>row.Name).Result;
        var failed=await Manager(provider).ExecuteEntityProjectionQueryAsync(ProjectionPlan(provider,protectedQuery),new[]{provider});
        Assert.AreEqual("CQL_QUERY_LIMIT",failed.ErrorCode,failed.Message+failed.Exception);
        var emptyQuery=source.Take(0).Result.Select(row=>row.Name).Result;
        foreach(var options in new[]{new CosmicEntityCompositionOptions(maximumInputPayloadBytes:1),new CosmicEntityCompositionOptions(maximumInputRows:1,maximumStageRows:1)})
        {
            // Two stages require two input-row visits even when the final output is empty.
            var bounded=source.Skip(0).Result.Take(0).Result.Select(row=>row.Name).Result;
            var limited=await Manager(provider).ExecuteEntityProjectionQueryAsync(ProjectionPlan(provider,bounded,options),new[]{provider});
            Assert.AreEqual("CQL_QUERY_LIMIT",limited.ErrorCode,limited.Message+limited.Exception);
        }
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        var cancelled=await provider.ExecuteCosmicEntityProjectionAsync(ProjectionPlan(provider,emptyQuery),cancellationToken:cancel.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED",cancelled.ErrorCode);
    }
}
