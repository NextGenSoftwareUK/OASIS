using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeCqlDescendantCertifiesTheActualDepth32AndNodeBoundary()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=new Holon{Id=Guid.NewGuid(),Name="owned deep boundary root"};
        global::OASIS.ProviderTests.Fixtures.CqlDescendantAcceptance.Configure(root);
        var saved=await provider.SaveHolonAsync(root,saveChildren:true);
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        await global::OASIS.ProviderTests.Fixtures.CqlDescendantAcceptance.VerifyAsync(provider,root);
    }

    [Fact]
    public async Task NativeCqlDescendantCancellationAfterNativeReaderReleasesOwnerAndCallbacks()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=NativeQueryGraph();
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        using var cancellation=new CancellationTokenSource();
        var observed=false;
        using var context=new GraphReadContext(TestConnectionString,new DescendantReaderInterceptor(()=>
        {
            observed=true;cancellation.Cancel();return Task.CompletedTask;
        }));
        var repository=new HolonRepository(context,()=>false);
        var plan=DescendantPlan(source);
        var read=await repository.ExecuteCosmicQueryAsync(plan,100,cancellation.Token);
        observed.Should().BeTrue();read.ErrorCode.Should().Be("CQL_QUERY_CANCELLED");
        read.Result.Should().BeNull();read.IsLoaded.Should().BeFalse();
        context.Database.CurrentTransaction.Should().BeNull();
        context.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);
        var fresh=await repository.ExecuteCosmicQueryAsync(plan,100,default);
        fresh.IsError.Should().BeFalse(fresh.Message+fresh.Exception);fresh.Result.Should().ContainSingle();
    }

    [Fact]
    public async Task NativeCqlDescendantSnapshotRetainsOldPathAcrossConcurrentReparent()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=NativeQueryGraph();var next=new Holon{Id=Guid.NewGuid(),Name="new parent"};
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(next,saveChildren:false)).IsError.Should().BeFalse();
        using(var configuration=new DataContext(TestConnectionString))
            configuration.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
        var moved=false;
        using var context=new GraphReadContext(TestConnectionString,new DescendantReaderInterceptor(async()=>
        {
            using var writer=new DataContext(TestConnectionString);
            var childId=source.Children.First().Id.ToString("D");var parentId=next.Id.ToString("D");
            await writer.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET ParentHolonId={parentId} WHERE Id={childId}");
            moved=true;
        }));
        var repository=new HolonRepository(context,()=>false);
        var old=await repository.ExecuteCosmicQueryAsync(DescendantPlan(source),100,default);
        moved.Should().BeTrue();old.IsError.Should().BeFalse(old.Message+old.Exception);old.Result.Should().ContainSingle();
        var fresh=await repository.ExecuteCosmicQueryAsync(DescendantPlan(source),100,default);
        fresh.IsError.Should().BeFalse(fresh.Message+fresh.Exception);fresh.Result.Should().BeEmpty();
        await global::OASIS.ProviderTests.Fixtures.CqlDescendantAcceptance.VerifyMovedSubtreeAsync(
            provider,source.Id,next.Id,false,"grandchild");
    }

    private sealed class DescendantReaderInterceptor(Func<Task> action) : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        private bool invoked;
        public override async ValueTask<System.Data.Common.DbDataReader> ReaderExecutedAsync(
            System.Data.Common.DbCommand command,Microsoft.EntityFrameworkCore.Diagnostics.CommandExecutedEventData eventData,
            System.Data.Common.DbDataReader result,CancellationToken cancellationToken=default)
        {
            if(!invoked && command.CommandText.Contains("WITH RECURSIVE",StringComparison.OrdinalIgnoreCase))
            {invoked=true;await action();}
            return result;
        }
    }

    private static CosmicQueryPlan<Holon> DescendantPlan(Holon root, int depth = 4, int nodes = 10,
        CosmicQueryReadAccess? access = null, string name = "grandchild")
    {
        var id = root.Id;
        var query = CosmicQuery<Holon>.Create();
        if (access != null) query = query.WithReadAccess(access).Result;
        var filtered = query.Where(parent => parent.Id == id)
            .WhereAnyDescendant(child => child.Name == name, CosmicQueryTraversalOptions.Create(depth, nodes).Result)
            .OrderBy(parent => parent.Name).Take(1);
        filtered.IsError.Should().BeFalse(filtered.Message);
        var plan = CosmicQueryPlanner.Plan(filtered.Result, ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, SQLLiteDBOASIS.CqlCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        return plan.Result;
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCqlDescendantValidatesAnEmptyParentsDeclaredManifest(bool direct)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=new Holon{Id=Guid.NewGuid(),Name="empty malformed owner"};
        (await provider.SaveHolonAsync(root,saveChildren:false)).IsError.Should().BeFalse();
        var id=root.Id.ToString("D");
        using var context=new DataContext(TestConnectionString);
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET PersistentValueJson=json_set(PersistentValueJson,'$.ChildRoleManifestJson',{"{broken"}) WHERE Id={id}");
        var rootId=root.Id;
        var query=CosmicQuery<Holon>.Create().Where(parent=>parent.Id==rootId).Result;
        var related=direct?query.WhereAnyChild(child=>child.Name=="missing"):
            query.WhereAnyDescendant(child=>child.Name=="missing",CosmicQueryTraversalOptions.Create().Result);
        var plan=CosmicQueryPlanner.Plan(related.Result,ProviderType.SQLLiteDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities)});
        var read=await provider.ExecuteCosmicQueryAsync(plan.Result);
        read.ErrorCode.Should().Be("CQL_DATA_INVALID");read.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeCqlDescendantWorksWithReadOnlyStorage()
    {
        var root=NativeQueryGraph();
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        }
        using var context=new DataContext($"Data Source={_databasePath};Mode=ReadOnly;Pooling=False");
        var result=await new HolonRepository(context,()=>false).ExecuteCosmicQueryAsync(DescendantPlan(root),100,default);
        result.IsError.Should().BeFalse(result.Message+result.Exception);result.Result.Should().ContainSingle();
        context.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);
    }

    [Fact]
    public async Task NativeCqlDescendantRetainsCallerTransactionOwnership()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=NativeQueryGraph();(await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        using var context=new DataContext(TestConnectionString);
        using var transaction=await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var id=root.Id.ToString("D");
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET Name={"caller pending"} WHERE Id={id}");
        var read=await new HolonRepository(context,()=>false).ExecuteCosmicQueryAsync(DescendantPlan(root),100,default);
        read.IsError.Should().BeFalse(read.Message+read.Exception);read.Result.Single().Name.Should().Be("caller pending");
        context.Database.CurrentTransaction.Should().BeSameAs(transaction);await transaction.RollbackAsync();
        using var control=new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Single(row=>row.Id==id).Name.Should().Be("root");
    }

    [Fact]
    public async Task NativeCqlDescendantDoesNotTraverseAnUnlistedPhysicalBranch()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=NativeQueryGraph();root.Children.First().Children.Single().Name="not target";
        (await CosmicManager(provider,true).SaveHolonAsync(root,Guid.NewGuid(),CreateNativeGraph(root),
            providerType:ProviderType.SQLLiteDBOASIS)).IsError.Should().BeFalse();
        var orphan=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="unlisted branch"};
        orphan.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=orphan.Id,Name="target"});
        (await provider.SaveHolonAsync(orphan)).IsError.Should().BeFalse();
        var read=await provider.ExecuteCosmicQueryAsync(DescendantPlan(root,nodes:3,name:"target"));
        read.IsError.Should().BeFalse(read.Message+read.Exception);read.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task NativeCqlDescendantIndependentClausesKeepExactTypedMetadata()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=NativeQueryGraph();root.Children.First().Children.Single().MetaData["precision"]=9007199254740993L;
        (await CosmicManager(provider,true).SaveHolonAsync(root,Guid.NewGuid(),CreateNativeGraph(root),
            providerType:ProviderType.SQLLiteDBOASIS)).IsError.Should().BeFalse();
        var id=root.Id;var limits=CosmicQueryTraversalOptions.Create(2,3).Result;
        var query=CosmicQuery<Holon>.Create().Where(parent=>parent.Id==id)
            .WhereAnyDescendant(child=>child.Name=="quest",limits)
            .WhereAnyDescendant(child=>CosmicQueryMetadata.Value<long>(child,"precision")==9007199254740993L,limits);
        var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.SQLLiteDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities)});
        var read=await provider.ExecuteCosmicQueryAsync(plan.Result);
        read.IsError.Should().BeFalse(read.Message+read.Exception);read.Result.Should().ContainSingle();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCqlDescendantMalformedMembershipReturnsTypedDataFailure(bool direct)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=NativeQueryGraph();
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        var id=root.Id.ToString("D");
        using var context=new DataContext(TestConnectionString);
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET PersistentValueJson=json_set(PersistentValueJson,'$.ChildRoleManifestJson',{"{broken"}) WHERE Id={id}");
        var rootId=root.Id;
        var query=CosmicQuery<Holon>.Create().Where(parent=>parent.Id==rootId).Result;
        var related=direct?query.WhereAnyChild(child=>child.Name=="quest"):
            query.WhereAnyDescendant(child=>child.Name=="grandchild",CosmicQueryTraversalOptions.Create().Result);
        related.IsError.Should().BeFalse(related.Message);
        var plan=CosmicQueryPlanner.Plan(related.Result,ProviderType.SQLLiteDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities)});
        var read=await provider.ExecuteCosmicQueryAsync(plan.Result);
        read.ErrorCode.Should().Be("CQL_DATA_INVALID");read.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeCqlDescendantFilteringPrecedesTheRootPage()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var first=NativeQueryGraph("A");first.Children.First().Children.Single().Name="different";
        var second=NativeQueryGraph("B");
        foreach(var root in new[]{first,second})
            (await CosmicManager(provider,true).SaveHolonAsync(root,Guid.NewGuid(),CreateNativeGraph(root),
                providerType:ProviderType.SQLLiteDBOASIS)).IsError.Should().BeFalse();
        var ids=new[]{first.Id,second.Id};
        var query=CosmicQuery<Holon>.Create().Where(root=>ids.Contains(root.Id))
            .WhereAnyDescendant(child=>child.Name=="grandchild",CosmicQueryTraversalOptions.Create().Result)
            .OrderBy(root=>root.Name).Take(1);
        var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.SQLLiteDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities)});
        var read=await provider.ExecuteCosmicQueryAsync(plan.Result);
        read.IsError.Should().BeFalse(read.Message+read.Exception);
        read.Result.Single().Name.Should().Be("B");
    }

    [Theory]
    [InlineData(1, false)] [InlineData(2, true)]
    public async Task NativeCqlDescendantRespectsTheDepthBoundaryWithoutLoadingGraphs(int depth, bool match)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph();
        (await CosmicManager(provider, true).SaveHolonAsync(root, Guid.NewGuid(), CreateNativeGraph(root),
            providerType: ProviderType.SQLLiteDBOASIS)).IsError.Should().BeFalse();
        var read = await provider.ExecuteCosmicQueryAsync(DescendantPlan(root, depth));
        read.IsError.Should().BeFalse(read.Message + read.Exception);
        read.Result.Should().HaveCount(match ? 1 : 0);
        if (match) read.Result.Single().Children.Should().BeEmpty();
    }

    [Fact]
    public async Task NativeCqlDescendantBudgetFailureDoesNotPublishOrPoisonTheNextRead()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph();
        (await CosmicManager(provider, true).SaveHolonAsync(root, Guid.NewGuid(), CreateNativeGraph(root),
            providerType: ProviderType.SQLLiteDBOASIS)).IsError.Should().BeFalse();
        var rejected = await provider.ExecuteCosmicQueryAsync(DescendantPlan(root, nodes: 2));
        rejected.ErrorCode.Should().Be("CQL_QUERY_LIMIT");
        rejected.Result.Should().BeNull(); rejected.IsLoaded.Should().BeFalse();
        var accepted = await provider.ExecuteCosmicQueryAsync(DescendantPlan(root, nodes: 3));
        accepted.IsError.Should().BeFalse(accepted.Message + accepted.Exception);
        accepted.Result.Should().ContainSingle();
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task NativeCqlDescendantPrunesPrivateIntermediateNodesBeforeMatching(int visibility)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor = Guid.NewGuid();
        var root = NativeQueryGraph(); root.IsPublic = true;
        root.Children.First().IsPublic = visibility == 1;
        root.Children.First().Children.Single().IsPublic = true;
        (await CosmicManager(provider, true).SaveHolonAsync(root, visibility == 2 ? actor : Guid.NewGuid(), CreateNativeGraph(root),
            providerType: ProviderType.SQLLiteDBOASIS)).IsError.Should().BeFalse();
        var read = await provider.ExecuteCosmicQueryAsync(DescendantPlan(root,
            access: CosmicQueryReadAccess.OwnerOrPublic(actor).Result));
        read.IsError.Should().BeFalse(read.Message + read.Exception);
        read.Result.Should().HaveCount(visibility == 0 ? 0 : 1);
    }

    [Fact]
    public async Task NativeCqlDescendantCycleRejectsWithoutRetainingTheConnectionFunction()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = new Holon { Id = Guid.NewGuid(), Name = "cycle" };
        (await provider.SaveHolonAsync(root, saveChildren: false)).IsError.Should().BeFalse();
        var id = root.Id.ToString("D");
        using var context = new DataContext(TestConnectionString);
        await context.Database.OpenConnectionAsync();
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET ParentHolonId={id}, PersistentValueJson=json_remove(PersistentValueJson,'$.ChildRoleManifestJson') WHERE Id={id}");
        var repository = new HolonRepository(context, () => false);
        var rejected = await repository.ExecuteCosmicQueryAsync(DescendantPlan(root, name: "cycle"), 100, CancellationToken.None);
        rejected.ErrorCode.Should().Be("CQL_DATA_INVALID");
        rejected.Result.Should().BeNull(); rejected.IsLoaded.Should().BeFalse();
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT cosmic_cql_traversal_guard(1)";
        await Assert.ThrowsAsync<SqliteException>(() => command.ExecuteScalarAsync());
    }
}
