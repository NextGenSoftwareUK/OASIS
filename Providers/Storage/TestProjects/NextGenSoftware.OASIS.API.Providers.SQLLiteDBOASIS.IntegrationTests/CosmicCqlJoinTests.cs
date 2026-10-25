using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static CosmicJoinPlan<Holon, Holon, TKey, TResult> JoinPlan<TKey, TResult>(SQLLiteDBOASIS provider,
        CosmicJoinSelection<Holon, Holon, TKey, TResult> query)
    {
        var native = (ICosmicQueryJoinStorageProvider)provider;
        var plan = CosmicJoinPlanner.Plan(query, native.QueryProviderType,
            new[] { new CosmicJoinTarget(native.QueryProviderType, native.QueryCapabilities, native.JoinCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        return plan.Result;
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeJoinPublishesDetachedFanoutAndUnmatchedPresence(bool outer)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var seed = new[] {
            new Holon { Name="coffee", Description="left", IsPublic=true },
            new Holon { Name="tea", Description="left", IsPublic=true },
            new Holon { Name="coffee", Description="right", IsPublic=true },
            new Holon { Name="coffee", Description="right", IsPublic=true } };
        (await provider.SaveHolonsAsync(seed, saveChildren:false)).IsError.Should().BeFalse();
        var left = CosmicQuery<Holon>.Create().Where(h => h.Description == "left").OrderBy(h => h.Name);
        var right = CosmicQuery<Holon>.Create().Where(h => h.Description == "right");
        var query = (outer ? left.LeftJoin(right, h => h.Name, h => h.Name) : left.Join(right, h => h.Name, h => h.Name))
            .Select((l,r) => new { Label=l.Name, RightLabel=r.Name, Present=r!=null }).Result;
        var result = await CosmicManager(provider,true).ExecuteJoinQueryAsync(JoinPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var rows = result.Result.Providers.Single().Rows;
        rows.Count.Should().Be(outer?3:2);
        rows.Count(row=>row.HasRight).Should().Be(2);
        rows.Should().OnlyContain(row=>row.HasRight==row.Value.Present);
        if(outer) rows.Last().Value.RightLabel.Should().BeNull();
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public async Task NativeJoinEnforcesEveryRemainingBudgetAndAllowsFreshReuse(int budget)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var row = new Holon { Name="coffee", IsPublic=true };
        (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();
        var id=row.Id;
        var source = CosmicQuery<Holon>.Create().Where(h=>h.Id==id);
        var query = source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
        var plan=JoinPlan(provider,query);
        var counts=new[]{10,10,10,10,10000,10000,10000}; counts[budget]=0;
        var failed=await provider.ExecuteCosmicJoinAsync(plan,
            new CosmicJoinExecutionLimits(counts[0],counts[1],counts[2],counts[3],counts[4],counts[5],counts[6]));
        failed.IsError.Should().BeTrue(); failed.ErrorCode.Should().Be("CQL_QUERY_LIMIT"); failed.IsLoaded.Should().BeFalse();
        var fresh=await CosmicManager(provider,true).ExecuteJoinQueryAsync(plan,new[]{provider});
        fresh.IsError.Should().BeFalse(fresh.Message+fresh.Exception);
        fresh.Result.Providers.Single().Rows.Single().Value.Should().Be("coffee");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeJoinValidatesUnmatchedSourceDomainsBeforePublishing(bool requiredMissing)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var rows=new[]{new Holon{Name="left",Description="L"},new Holon{Name="right",Description="R"}};
        if(!requiredMissing)rows[1].MetaData["stock"]="wrong-domain";
        (await provider.SaveHolonsAsync(rows,saveChildren:false)).IsError.Should().BeFalse();
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Description=="L");
        var right=CosmicQuery<Holon>.Create().Where(h=>h.Description=="R");
        var query=left.LeftJoin(right,h=>h.Name,h=>h.Name)
            .Select((l,r)=>new {l.Name,Stock=(long?)CosmicQueryMetadata.Value<long>(r,"stock")}).Result;
        var result=await CosmicManager(provider,true).ExecuteJoinQueryAsync(JoinPlan(provider,query),new[]{provider});
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_DATA_INVALID"); result.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task NativeJoinFiltersBothActorsBeforeMatchingAndPaging()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor=Guid.NewGuid();var other=Guid.NewGuid();
        var rows=new[]{new Holon{Name="coffee",Description="L",CreatedByAvatarId=actor},
            new Holon{Name="coffee",Description="R",CreatedByAvatarId=other},
            new Holon{Name="coffee",Description="R",CreatedByAvatarId=other,IsPublic=true}};
        (await provider.SaveHolonsAsync(rows,saveChildren:false)).IsError.Should().BeFalse();
        var source=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result);
        var left=source.Where(h=>h.Description=="L");
        var right=source.Where(h=>h.Description=="R").Take(1);
        var query=left.Join(right,h=>h.Name,h=>h.Name).Select((l,r)=>r.Id).Result;
        var result=await CosmicManager(provider,true).ExecuteJoinQueryAsync(JoinPlan(provider,query),new[]{provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        result.Result.Providers.Single().Rows.Single().Value.Should().Be(rows[2].Id);
    }

    [Fact]
    public async Task NativeJoinCancellationPublishesNothingAndReleasesReadOwner()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=CosmicQuery<Holon>.Create();
        var query=source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
        var plan=JoinPlan(provider,query);
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        var result=await provider.ExecuteCosmicJoinAsync(plan,
            new CosmicJoinExecutionLimits(10,10,10,10,10000,10000,10000),cancelled.Token);
        result.ErrorCode.Should().Be("CQL_QUERY_CANCELLED");result.IsLoaded.Should().BeFalse();
        var fresh=await CosmicManager(provider,true).ExecuteJoinQueryAsync(plan,new[]{provider});
        fresh.IsError.Should().BeFalse(fresh.Message+fresh.Exception);
        fresh.Result.Providers.Single().Rows.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    public async Task NativeJoinUsesExactMetadataKeyDomains(int domain)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        switch(domain)
        {
            case 0: await Check(9007199254740993L,9007199254740992L);break;
            case 1: await Check(ulong.MaxValue,ulong.MaxValue-1);break;
            case 2: await Check(123456789.123456789123456789m,123456789.123456789123456788m);break;
            case 3: await Check(1.0000000000000002d,1d);break;
            case 4: await Check(1.0000001f,1f);break;
            case 5: await Check(new DateTime(638900000000000001,DateTimeKind.Utc),new DateTime(638900000000000000,DateTimeKind.Utc));break;
            case 6: await Check(Guid.Parse("a0000000-0000-0000-0000-000000000001"),Guid.Parse("a0000000-0000-0000-0000-000000000002"));break;
            case 7: await Check(true,false);break;
            case 8: await Check("coffee' OR 1=1 --","COFFEE' OR 1=1 --");break;
            case 9: await Check(int.MaxValue,int.MaxValue-1);break;
            case 10: await Check((long?)9007199254740993L,(long?)null);break;
            default: await Check(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS,
                NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS);break;
        }

        async Task Check<T>(T key,T different)
        {
            var left=new Holon{Name="L",MetaData=new(){{"key",key!}}};
            var right=new Holon{Name="R",MetaData=new(){{"key",key!}}};
            var other=new Holon{Name="R",MetaData=new(){{"key",different!}}};
            (await provider.SaveHolonsAsync(new[]{left,right,other},saveChildren:false)).IsError.Should().BeFalse();
            var l=CosmicQuery<Holon>.Create().Where(h=>h.Name=="L");
            var r=CosmicQuery<Holon>.Create().Where(h=>h.Name=="R");
            var selected=l.Join(r,h=>CosmicQueryMetadata.Value<T>(h,"key"),h=>CosmicQueryMetadata.Value<T>(h,"key"))
                .Select((a,b)=>new {Left=CosmicQueryMetadata.Value<T>(a,"key"),Right=CosmicQueryMetadata.Value<T>(b,"key")});
            selected.IsError.Should().BeFalse(selected.Message);
            var result=await CosmicManager(provider,true).ExecuteJoinQueryAsync(JoinPlan(provider,selected.Result),new[]{provider});
            result.IsError.Should().BeFalse(result.Message+result.Exception);
            var value=result.Result.Providers.Single().Rows.Single().Value;
            value.Left.Should().Be(key);value.Right.Should().Be(key);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeJoinPreservesMissingAndNullOrderingBeforeSourceWindows(bool descending)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var missing=new Holon {Id=Guid.Parse("20000000-0000-0000-0000-000000000001"),Name="coffee",Description="L"};
        var empty=new Holon {Id=Guid.Parse("10000000-0000-0000-0000-000000000001"),Name="coffee",Description="L",MetaData=new(){{"rank",null!}}};
        var right=new Holon {Name="coffee",Description="R"};
        (await provider.SaveHolonsAsync(new[]{missing,empty,right},saveChildren:false)).IsError.Should().BeFalse();
        var source=CosmicQuery<Holon>.Create().Where(h=>h.Description=="L");
        var ordered=descending?source.OrderByDescending(h=>CosmicQueryMetadata.Value<long>(h,"rank"))
            :source.OrderBy(h=>CosmicQueryMetadata.Value<long>(h,"rank"));
        var selected=ordered.Join(CosmicQuery<Holon>.Create().Where(h=>h.Description=="R"),h=>h.Name,h=>h.Name)
            .Select((l,r)=>l.Id).Result;
        var result=await CosmicManager(provider,true).ExecuteJoinQueryAsync(JoinPlan(provider,selected),new[]{provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        result.Result.Providers.Single().Rows.Select(row=>row.Value)
            .Should().Equal(descending?new[]{empty.Id,missing.Id}:new[]{missing.Id,empty.Id});
        var window=ordered.Skip(1).Take(1).Join(CosmicQuery<Holon>.Create().Where(h=>h.Description=="R"),h=>h.Name,h=>h.Name)
            .Select((l,r)=>l.Id).Result;
        var page=await CosmicManager(provider,true).ExecuteJoinQueryAsync(JoinPlan(provider,window),new[]{provider});
        page.IsError.Should().BeFalse(page.Message+page.Exception);
        page.Result.Providers.Single().Rows.Single().Value.Should().Be(descending?missing.Id:empty.Id);
    }

    [Fact]
    public async Task NativeJoinCompositeKeysRequireEveryComponent()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var rows=new[]{new Holon{Name="L",Description="coffee",MetaData=new(){{"region",7L}}},
            new Holon{Name="R",Description="coffee",MetaData=new(){{"region",7L}}},
            new Holon{Name="R",Description="coffee",MetaData=new(){{"region",8L}}}};
        (await provider.SaveHolonsAsync(rows,saveChildren:false)).IsError.Should().BeFalse();
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Name=="L");
        var right=CosmicQuery<Holon>.Create().Where(h=>h.Name=="R");
        var selected=left.Join(right,h=>new{h.Description,Region=CosmicQueryMetadata.Value<long>(h,"region")},
            h=>new{h.Description,Region=CosmicQueryMetadata.Value<long>(h,"region")}).Select((l,r)=>r.Id).Result;
        var result=await CosmicManager(provider,true).ExecuteJoinQueryAsync(JoinPlan(provider,selected),new[]{provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        result.Result.Providers.Single().Rows.Single().Value.Should().Be(rows[1].Id);
    }

    [Fact]
    public async Task NativeJoinReusesCallerSnapshotAcrossAcknowledgedConcurrentMutation()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var left=new Holon{Name="coffee",Description="L"};
        var right=new Holon{Name="coffee",Description="R"};
        (await provider.SaveHolonsAsync(new[]{left,right},saveChildren:false)).IsError.Should().BeFalse();
        var query=CosmicQuery<Holon>.Create().Where(h=>h.Description=="L")
            .Join(CosmicQuery<Holon>.Create().Where(h=>h.Description=="R"),h=>h.Name,h=>h.Name)
            .Select((l,r)=>r.Name).Result;
        var plan=JoinPlan(provider,query);
        using(var context=new DataContext(TestConnectionString))
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");
            await context.Database.OpenConnectionAsync();
            var connection=(SqliteConnection)context.Database.GetDbConnection();
            using var snapshot=connection.BeginTransaction(System.Data.IsolationLevel.Serializable,deferred:true);
            using var caller=context.Database.UseTransaction(snapshot);
            (await context.Database.SqlQueryRaw<long>("SELECT count(*) AS Value FROM Holon").SingleAsync()).Should().Be(2);
            using(var writer=new DataContext(TestConnectionString))
            {
                var id=right.Id.ToString("D");
                (await writer.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET Name={"tea"} WHERE Id={id}")).Should().Be(1);
            }
            var native=await new HolonRepository(context,()=>false).ExecuteCosmicJoinAsync(plan,
                new CosmicJoinExecutionLimits(10,10,10,10,10000,10000,10000));
            native.IsError.Should().BeFalse(native.Message+native.Exception);
            var selected=CosmicJoinResults.Materialize(query,native.Result);
            selected.IsError.Should().BeFalse(selected.Message);
            selected.Result.Single().Value.Should().Be("coffee");
            context.Database.CurrentTransaction.Should().NotBeNull("the caller retains ownership");
        }
        var fresh=await CosmicManager(provider,true).ExecuteJoinQueryAsync(plan,new[]{provider});
        fresh.IsError.Should().BeFalse(fresh.Message+fresh.Exception);
        fresh.Result.Providers.Single().Rows.Should().BeEmpty();
    }
}
