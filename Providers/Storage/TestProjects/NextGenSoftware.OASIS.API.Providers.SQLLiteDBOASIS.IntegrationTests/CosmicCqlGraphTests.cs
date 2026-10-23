using FluentAssertions;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
    private static Holon NativeQueryGraph(string name = "root")
    {
        var root = new Holon { Id = Guid.NewGuid(), Name = name };
        var quest = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "quest", HolonType = HolonType.Quest };
        quest.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = quest.Id, Name = "grandchild", HolonType = HolonType.Quest });
        root.Children.Add(quest);
        root.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "cafe", HolonType = HolonType.Cafe });
        return root;
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCqlAnyChildReparentingRespectsCurrentParentAndNewMembership(bool declaredNewOwner)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var old=new Holon{Id=Guid.NewGuid(),Name="old",IsPublic=true};
        var next=new Holon{Id=Guid.NewGuid(),Name="next",IsPublic=true};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=old.Id,Name="match",IsPublic=true};
        old.Children.Add(child);
        old.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
            1,old.Id,new[]{new HolonChildRole("Children",new[]{child.Id})}));
        next.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
            1,next.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>())}));
        (await provider.SaveHolonAsync(old)).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(next)).IsError.Should().BeFalse();
        using(var context=new DataContext(TestConnectionString))
        {
            var childId=child.Id.ToString("D");var nextId=next.Id.ToString("D");
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET ParentHolonId={nextId} WHERE Id={childId}");
            if(!declaredNewOwner)await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET PersistentValueJson=json_remove(PersistentValueJson,'$.ChildRoleManifestJson') WHERE Id={nextId}");
        }
        var ids=new[]{old.Id,next.Id};
        var query=CosmicQuery<Holon>.Create().Where(root=>ids.Contains(root.Id)).WhereAnyChild(item=>item.Name=="match");
        var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.SQLLiteDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities)});
        plan.IsError.Should().BeFalse(plan.Message);
        var read=await provider.ExecuteCosmicQueryAsync(plan.Result);
        read.IsError.Should().BeFalse(read.Message+read.Exception);
        read.Result.Select(row=>row.Id).Should().Equal(declaredNewOwner?Array.Empty<Guid>():new[]{next.Id});
    }

    [Fact]
    public async Task NativeCqlAnyChildSeparatesCorrelatedAliasesAndPreservesExactMetadata()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=new Holon{Id=Guid.NewGuid(),Name="parent",IsPublic=true};
        root.MetaData["precision"]=0L;
        root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="match",IsPublic=true});
        var extra=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="different child",IsPublic=true};
        extra.MetaData["precision"]=9007199254740993L;
        extra.MetaData["location"]=new Dictionary<string,object>{{"city","London"}};
        root.Children.Add(extra);
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        var id=root.Id;
        var query=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
            .Where(row=>row.Id==id).WhereAnyChild(item=>item.Name=="match")
            .WhereAnyChild(item=>CosmicQueryMetadata.Value<long>(item,"precision")==9007199254740993L &&
                CosmicQueryMetadata.Value<string>(item,"location","city")=="London");
        query.IsError.Should().BeFalse(query.Message);
        var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.SQLLiteDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities)});
        plan.IsError.Should().BeFalse(plan.Message);
        var read=await provider.ExecuteCosmicQueryAsync(plan.Result);
        read.IsError.Should().BeFalse(read.Message+read.Exception);
        read.Result.Single().Id.Should().Be(root.Id);
    }

    [Fact]
    public async Task NativeCqlAnyChildUsesMembershipAndVisibilityBeforeRootPaging()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor=Guid.NewGuid();var other=Guid.NewGuid();
        var roots=Enumerable.Range(0,4).Select(index=>new Holon{Id=Guid.NewGuid(),Name=((char)('A'+index)).ToString(),IsPublic=true}).ToArray();
        for(var index=0;index<roots.Length;index++)
        {
            var root=roots[index];
            root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name=index==3?"no":"match",
                IsPublic=index is 1 or 3,CreatedByAvatarId=index==2?actor:other});
            root.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
                1,root.Id,new[]{new HolonChildRole("Children",root.Children.Select(child=>child.Id))}));
            var saved=await provider.SaveHolonAsync(root);saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        }
        // A physical parent link outside the canonical role membership cannot
        // turn D into a matching parent. No hidden child can make A match.
        var orphan=new Holon{Id=Guid.NewGuid(),ParentHolonId=roots[3].Id,Name="match",IsPublic=true};
        (await provider.SaveHolonAsync(orphan,saveChildren:false)).IsError.Should().BeFalse();
        var ids=roots.Select(root=>root.Id).ToArray();
        var query=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .Where(root=>ids.Contains(root.Id)).WhereAnyChild(child=>child.Name=="match")
            .OrderBy(root=>root.Name).Skip(1).Take(1);
        query.IsError.Should().BeFalse(query.Message);
        var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.SQLLiteDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities)});
        plan.IsError.Should().BeFalse(plan.Message);
        var read=await provider.ExecuteCosmicQueryAsync(plan.Result);
        read.IsError.Should().BeFalse(read.Message+read.Exception);
        read.Result.Single().Name.Should().Be("C");
    }

    [Fact]
    public async Task NativeCqlGraphRejectsACorruptParentLinkCycle()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new Holon{Id=Guid.NewGuid(),Name="owned cyclic fixture"};
        (await provider.SaveHolonAsync(source,saveChildren:false)).IsError.Should().BeFalse();
        var id=source.Id.ToString("D");
        using var context=new DataContext(TestConnectionString);
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET ParentHolonId={id}, PersistentValueJson=json_remove(PersistentValueJson,'$.ChildRoleManifestJson') WHERE Id={id}");
        var repository=new HolonRepository(context,()=>false);
        var read=await repository.ExecuteCosmicQueryAsync(NativeGraphPlan(source,CosmicQueryGraphOptions.Create().Result),100,default);
        read.IsError.Should().BeTrue();read.ErrorCode.Should().Be("CQL_DATA_INVALID");
        read.Result.Should().BeNull();read.IsLoaded.Should().BeFalse();
        context.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
    }

    [Fact]
    public async Task NativeCqlGraphCancellationAtChildCommandReleasesItsReadOwner()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=NativeQueryGraph();
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        using var cancellation=new CancellationTokenSource();
        var intercepted=false;
        using var context=new GraphReadContext(TestConnectionString,new GraphReadInterceptor(()=>
        {
            intercepted=true;cancellation.Cancel();return Task.CompletedTask;
        }));
        var repository=new HolonRepository(context,()=>false);
        var plan=NativeGraphPlan(source,CosmicQueryGraphOptions.Create().Result);
        var read=await repository.ExecuteCosmicQueryAsync(plan,100,cancellation.Token);
        intercepted.Should().BeTrue();read.ErrorCode.Should().Be("CQL_QUERY_CANCELLED");
        read.Result.Should().BeNull();read.IsLoaded.Should().BeFalse();
        context.Database.CurrentTransaction.Should().BeNull();
        context.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
        var retry=await repository.ExecuteCosmicQueryAsync(plan,100,default);
        retry.IsError.Should().BeFalse(retry.Message+retry.Exception);
        retry.Result.Single().Children.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCqlGraphHydratesNamedTypedRoles(bool accessBound)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=NativeQueryGraph();
        var owner=(global::OASIS.ProviderTests.Fixtures.CqlRoleOwner)global::OASIS.ProviderTests.Fixtures.CqlCustomRoleGraph.CreateRoot();
        owner.Id=source.Id;owner.Children=source.Children;
        global::OASIS.ProviderTests.Fixtures.CqlCustomRoleGraph.Configure(owner);
        var manager=CosmicManager(provider,true);
        var seeded=await manager.SaveHolonAsync(owner,Guid.NewGuid(),CreateNativeGraph(owner),providerType:ProviderType.SQLLiteDBOASIS);
        seeded.IsError.Should().BeFalse(seeded.Message+seeded.Exception);
        await global::OASIS.ProviderTests.Fixtures.CqlCustomRoleGraph.VerifyAsync(manager,provider,owner,accessBound);
    }

    [Theory]
    [InlineData(true,true)] [InlineData(false,true)]
    [InlineData(true,false)] [InlineData(false,false)]
    public async Task NativeCqlGraphAccessFiltersBeforeChildBudget(bool visible,bool manifested)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=NativeQueryGraph();source.IsPublic=true;source.Children.First().IsPublic=visible;
        var seeded=manifested ? await CosmicManager(provider,true).SaveHolonAsync(source,Guid.NewGuid(),
            CreateNativeGraph(source),providerType:ProviderType.SQLLiteDBOASIS) : await provider.SaveHolonAsync(source);
        seeded.IsError.Should().BeFalse(seeded.Message+seeded.Exception);
        var policy=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var plan=NativeGraphPlan(source,CosmicQueryGraphOptions.Create(maximumNodes:visible?2:1).Result,policy);
        var read=await provider.ExecuteCosmicQueryAsync(plan);
        read.IsError.Should().BeFalse(read.Message+read.Exception);
        var root=read.Result.Single();
        root.Children.Should().HaveCount(visible?1:0);
        root.Children.All(child=>child.IsPublic && !child.Children.Any()).Should().BeTrue();
        if(manifested)Core.Serialization.HolonChildRoleManifestJson.Deserialize(((Holon)root).ChildRoleManifestJson).Roles.Single().ChildIds.Should().HaveCount(2);

        // Exercise the public SDK native -> detached -> typed graph pipeline.
        var published=await CosmicManager(provider,true).ExecuteQueryAsync(plan,
            new ICosmicQueryStorageProvider[]{provider});
        published.IsError.Should().BeFalse(published.Message+published.Exception);
        var typed=published.Result.Providers.Single().Rows.Single();
        typed.IsPublic.Should().BeTrue();
        typed.Children.Should().HaveCount(visible?1:0);
        HolonChangeTracking.HasChanges(typed).Should().BeFalse();
    }

    private static CosmicQuery<Holon> BindNativeGraphPolicy(CosmicQuery<Holon> query,CosmicQueryReadAccess policy)
    {
        var bound=query.WithReadAccess(policy);
        if(bound.IsError)throw new InvalidOperationException(bound.Message);
        return bound.Result;
    }

    private static CosmicQueryPlan<Holon> NativeGraphPlan(Holon root, CosmicQueryGraphOptions options,CosmicQueryReadAccess? access=null)
    {
        var id = root.Id;
        var query = CosmicQuery<Holon>.Create().Where(row => row.Id == id).WithChildren(options);
        query.IsError.Should().BeFalse(query.Message);
        var plan = CosmicQueryPlanner.Plan(access==null ? query.Result : BindNativeGraphPolicy(query.Result,access), ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, SQLLiteDBOASIS.CqlCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        return plan.Result;
    }

    private static CosmicWriteOptions CreateNativeGraph(Holon root)
    {
        // These fixtures deliberately assign IDs. Explicit null revisions mean
        // create-only, rather than falsely claiming an accepted store revision.
        var expected = new Dictionary<Guid, Guid?>();
        var pending = new Stack<IHolon>();
        pending.Push(root);
        while (pending.Count != 0)
        {
            var node = pending.Pop();
            expected.Add(node.Id, null);
            foreach (var child in node.AllChildren) pending.Push(child);
        }
        return new CosmicWriteOptions(Guid.NewGuid(), expected);
    }

    [Theory]
    [InlineData(true, 0, true)]
    [InlineData(true, 1, false)]
    [InlineData(true, 2, true)]
    [InlineData(false, 0, false)]
    [InlineData(false, 2, false)]
    public async Task NativeCqlGraphsRespectDepthAndRecursiveOptions(bool recursive, int depth, bool grandchild)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = NativeQueryGraph();
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        var options = CosmicQueryGraphOptions.Create(recursive: recursive, maxChildDepth: depth).Result;
        ICosmicQueryStorageProvider adapter = provider;
        var result = await CosmicManager(provider, true).ExecuteQueryAsync(NativeGraphPlan(source, options), new[] { adapter });

        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var root = result.Result.Providers.Single().Rows.Single();
        root.Children.Should().HaveCount(2);
        root.Children.Single(child => child.Name == "quest").Children.Should().HaveCount(grandchild ? 1 : 0);
        HolonChangeTracking.HasChanges(root).Should().BeFalse();
        root.Children.Should().OnlyContain(child => child.ParentHolonId == root.Id);
    }

    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task NativeCqlGraphTypeSelectionPreservesHiddenMembershipOnRootSave(bool manifested,bool accessBound)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = NativeQueryGraph();
        source.IsPublic=true;source.Children.First().IsPublic=true;
        var manager = CosmicManager(provider, true);
        var seeded = manifested ? await manager.SaveHolonAsync(source, Guid.NewGuid(),
            CreateNativeGraph(source), providerType: ProviderType.SQLLiteDBOASIS)
            : await provider.SaveHolonAsync(source);
        seeded.IsError.Should().BeFalse(seeded.Message + seeded.Exception);
        var hidden = source.Children.Single(child => child.HolonType == HolonType.Cafe).Id;
        ICosmicQueryStorageProvider adapter = provider;
        var baseline=await provider.ExecuteCosmicQueryAsync(NativeGraphPlan(source,CosmicQueryGraphOptions.Create().Result));
        baseline.IsError.Should().BeFalse(baseline.Message);
        var childState=baseline.Result.Single().Children.ToDictionary(child=>child.Id,
            child=>(child.Version,CosmicConcurrency.GetProviderRevisionId(child,ProviderType.SQLLiteDBOASIS)));
        var policy=accessBound?CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result:null;
        var result = await manager.ExecuteQueryAsync(NativeGraphPlan(source,
            CosmicQueryGraphOptions.Create(childType:accessBound?HolonType.All:HolonType.Quest).Result,policy), new[] { adapter });

        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var root = result.Result.Providers.Single().Rows.Single();
        root.Children.Should().ContainSingle().Which.HolonType.Should().Be(HolonType.Quest);
        if (manifested) root.ChildRoleManifestJson.Should().Contain(hidden.ToString("D"));
        else root.ChildRoleManifestJson.Should().BeNull();
        root.AcceptChanges();
        root.Name = "edited root only";
        var version = root.Version;
        var rejected = await manager.SaveHolonAsync(root, Guid.NewGuid(), saveChildren: true, providerType: ProviderType.SQLLiteDBOASIS);
        rejected.IsError.Should().BeTrue("a filtered view cannot replace unselected relationships");
        rejected.IsSaved.Should().BeFalse();
        root.Version.Should().Be(version);
        var saved = await manager.SaveHolonAsync(root, Guid.NewGuid(), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        var persisted = await provider.LoadHolonAsync(source.Id);
        persisted.Result.Children.Should().HaveCount(2);
        persisted.Result.Children.Should().Contain(child => child.Id == hidden);
        persisted.Result.Version.Should().Be(version+1);
        foreach(var child in persisted.Result.Children)
            (child.Version,CosmicConcurrency.GetProviderRevisionId(child,ProviderType.SQLLiteDBOASIS)).Should().Be(childState[child.Id]);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCqlGraphLimitsRejectWithoutPartialRowsAndReleaseTheirReadOwner(bool payload)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = NativeQueryGraph();
        source.Description = new string('\u20ac', 2048);
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        var options = CosmicQueryGraphOptions.Create(maximumNodes: payload ? 100 : 1,
            maximumPayloadBytes: payload ? 2048 : 1024 * 1024).Result;
        ICosmicQueryStorageProvider adapter = provider;
        var result = await CosmicQueryExecutor.ExecuteAsync(NativeGraphPlan(source, options), new[] { adapter });

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("CQL_QUERY_LIMIT");
        result.Result.Should().BeNull();
        // A failed graph must not leave a connection-local progress hook or read transaction behind.
        var id = source.Id;
        var control = await CosmicQueryExecutor.ExecuteAsync(CqlPlan(row => row.Id == id), new[] { adapter });
        control.IsError.Should().BeFalse(control.Message + control.Exception);
        control.Result.TotalRows.Should().Be(1);
    }

    [Fact]
    public async Task NativeCqlGraphExpandsOnlyPagedRoots()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var first = NativeQueryGraph("A-root");
        var second = NativeQueryGraph("B-root");
        (await provider.SaveHolonAsync(first)).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(second)).IsError.Should().BeFalse();
        var query = CosmicQuery<Holon>.Create().Where(row => row.ParentHolonId == Guid.Empty)
            .OrderBy(row => row.Name).Skip(1).Take(1)
            .WithChildren(CosmicQueryGraphOptions.Create(maximumNodes: 4).Result);
        var plan = CosmicQueryPlanner.Plan(query.Result, ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, SQLLiteDBOASIS.CqlCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        ICosmicQueryStorageProvider adapter = provider;
        var result = await CosmicQueryExecutor.ExecuteAsync(plan.Result, new[] { adapter });

        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var root = result.Result.Providers.Single().Rows.Single();
        root.Id.Should().Be(second.Id);
        root.Children.Should().HaveCount(2);
    }

    [Fact]
    public async Task NativeCqlGraphKeepsOneSnapshotWhenAChildChangesAfterTheRootPage()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = NativeQueryGraph();
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        var childId = source.Children[0].Id.ToString("D");
        using (var configure = new DataContext(TestConnectionString))
            configure.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
        var changed = false;
        var interceptor = new GraphReadInterceptor(async () =>
        {
            using var writer = new DataContext(TestConnectionString);
            await writer.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET Name={"newer child"} WHERE Id={childId}");
            changed = true;
        });
        using var context = new GraphReadContext(TestConnectionString, interceptor);
        var repository = new HolonRepository(context, () => false);
        var result = await repository.ExecuteCosmicQueryAsync(
            NativeGraphPlan(source, CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot).Result), 100, default);

        changed.Should().BeTrue("the concurrent write must commit before native child enumeration");
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Single().Children.Single(child => child.Id.ToString("D") == childId).Name.Should().Be("quest");
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Single(row => row.Id == childId).Name.Should().Be("newer child");
    }

    [Fact]
    public async Task NativeCqlGraphCanUseAReadOnlyConnection()
    {
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveHolonAsync(NativeQueryGraph())).IsError.Should().BeFalse();
        }
        using var context = new DataContext($"Data Source={_databasePath};Mode=ReadOnly;Pooling=False");
        var sourceId = Guid.Parse(context.Holons.AsNoTracking().Single(row => row.Name == "root").Id);
        var repository = new HolonRepository(context, () => false);
        var result = await repository.ExecuteCosmicQueryAsync(NativeGraphPlan(new Holon { Id = sourceId },
            CosmicQueryGraphOptions.Create().Result), 100, default);

        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Single().Children.Should().HaveCount(2);
        context.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
    }

    [Fact]
    public async Task NativeCqlGraphDoesNotCommitAnExistingCallerTransaction()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = NativeQueryGraph();
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString);
        using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var id = source.Id.ToString("D");
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET Name={"caller pending"} WHERE Id={id}");
        var repository = new HolonRepository(context, () => false);
        var result = await repository.ExecuteCosmicQueryAsync(
            NativeGraphPlan(source, CosmicQueryGraphOptions.Create().Result), 100, default);

        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Single().Name.Should().Be("caller pending");
        context.Database.CurrentTransaction.Should().BeSameAs(transaction);
        await transaction.RollbackAsync();
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Single(row => row.Id == id).Name.Should().Be("root");
    }

    [Fact]
    public async Task NativeCqlGraphRejectsAWeakerCallerTransaction()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = NativeQueryGraph();
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString + ";Cache=Shared");
        using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
        var repository = new HolonRepository(context, () => false);
        var result = await repository.ExecuteCosmicQueryAsync(
            NativeGraphPlan(source, CosmicQueryGraphOptions.Create().Result), 100, default);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("CQL_CONSISTENCY_UNSUPPORTED");
        context.Database.CurrentTransaction.Should().BeSameAs(transaction);
    }

    [Fact]
    public async Task NativeCqlGraphBatchesLargePersistedMembershipWithoutLosingItsOrder()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Id = Guid.NewGuid(), Name = "large owner" };
        for (var index = 0; index < 150; index++)
            source.Children.Add(new Holon { Id = Guid.NewGuid(), Name = index.ToString("D3"), ParentHolonId = source.Id });
        var manager = CosmicManager(provider, true);
        var seeded = await manager.SaveHolonAsync(source, Guid.NewGuid(), CreateNativeGraph(source),
            providerType: ProviderType.SQLLiteDBOASIS);
        seeded.IsError.Should().BeFalse(seeded.Message + seeded.Exception);
        ICosmicQueryStorageProvider adapter = provider;
        var result = await manager.ExecuteQueryAsync(NativeGraphPlan(source,
            CosmicQueryGraphOptions.Create(maximumNodes: 151).Result), new[] { adapter });

        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Children.Select(child => child.Name)
            .Should().Equal(Enumerable.Range(0, 150).Select(index => index.ToString("D3")));
    }

    private sealed class GraphReadContext : DataContext
    {
        private readonly DbCommandInterceptor _interceptor;
        internal GraphReadContext(string connection, DbCommandInterceptor interceptor) : base(connection) { _interceptor = interceptor; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        { base.OnConfiguring(options); options.AddInterceptors(_interceptor); }
    }

    private sealed class GraphReadInterceptor : DbCommandInterceptor
    {
        private readonly Func<Task> _write;
        private bool _invoked;
        internal GraphReadInterceptor(Func<Task> write) { _write = write; }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (!_invoked && command.CommandText.Contains("$graphLimit", StringComparison.Ordinal))
            {
                _invoked = true;
                await _write();
            }
            return result;
        }
    }
}
