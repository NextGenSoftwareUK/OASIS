using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryGraphExecutionTests
{
    private static Holon Graph()
    {
        var root = new Holon { Id = Guid.NewGuid(), Name = "root" };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "child" };
        child.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = child.Id, Name = "grandchild" });
        root.Children.Add(child);
        return root;
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BothBaseMappersPreserveStoredVisibility(bool isPublic)
    {
        var source=new Holon{Id=Guid.NewGuid(),IsPublic=isPublic,CreatedByAvatarId=Guid.NewGuid()};
        var plain=new Holon{IsPublic=!isPublic};
        var typed=new CountingRoot{IsPublic=!isPublic};
        Core.Helpers.Mapper.MapBaseHolonProperties(source,plain,mapChildren:false);
        Core.Helpers.Mapper<IHolon,CountingRoot>.MapBaseHolonProperties(source,typed);
        Assert.Equal(isPublic,plain.IsPublic);Assert.Equal(isPublic,typed.IsPublic);
        Assert.Equal(source.CreatedByAvatarId,plain.CreatedByAvatarId);
        Assert.Equal(source.CreatedByAvatarId,typed.CreatedByAvatarId);
    }

    // Boundary fixture while public SDK/HTTP access-graph admission stays closed.
    private static CosmicQuery<T> BindGraphPolicy<T>(CosmicQuery<T> query, CosmicQueryReadAccess policy) where T:IHolon
        => (CosmicQuery<T>)typeof(CosmicQuery<T>).GetConstructors(
            System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Single()
            .Invoke(new object?[]{query.Scope,query.Predicates.ToArray(),query.Orderings.ToArray(),
                query.Offset,query.Limit,query.GraphOptions,policy});

    [Theory]
    [InlineData(1)] [InlineData(2)]
    public async Task ForeignDescendantsRejectBeforeAnyTypedRootConstructor(int depth)
    {
        var source=Graph();source.IsPublic=true;
        source.Children[0].IsPublic=depth!=1;
        source.Children[0].Children[0].IsPublic=depth!=2;
        var adapter=new Adapter(source);
        var query=BindGraphPolicy(CosmicQuery<CountingRoot>.Create()
            .WithChildren(CosmicQueryGraphOptions.Create().Result).Result,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result);
        var plan=CosmicQueryPlanner.Plan(query,adapter.QueryProviderType,
            new[]{new CosmicQueryTarget(adapter.QueryProviderType,adapter.QueryCapabilities)});
        Assert.False(plan.IsError,plan.Message);
        CountingRoot.Constructions=0;
        var read=await Manager().ExecuteQueryAsync(plan.Result,new[]{adapter});
        Assert.True(read.IsError);
        Assert.Equal("CQL_VISIBILITY_VIOLATION",read.ErrorCode);
        Assert.Null(read.Result);
        Assert.Equal(0,CountingRoot.Constructions);
    }

    [Fact]
    public async Task GraphEdgesAndMetadataAreDetachedBeforePublication()
    {
        var source = Graph();
        source.Children[0].MetaData["marker"] = "original";
        var adapter = new Adapter(source);
        var result = await CosmicQueryExecutor.ExecuteAsync(Plan(CosmicQueryGraphOptions.Create().Result, adapter), new[] { adapter });

        Assert.False(result.IsError, result.Message + result.Exception);
        var root = Assert.Single(Assert.Single(result.Result.Providers).Rows);
        var child = Assert.Single(root.Children);
        Assert.Equal("grandchild", Assert.Single(child.Children).Name);
        Assert.NotSame(source, root);
        Assert.NotSame(source.Children, root.Children);
        Assert.NotSame(source.Children[0], child);
        Assert.NotSame(source.Children[0].MetaData, child.MetaData);
        child.Name = "edited";
        child.MetaData["marker"] = "edited";
        Assert.Equal("child", source.Children[0].Name);
        Assert.Equal("original", source.Children[0].MetaData["marker"]);
        Assert.Equal(1, result.Result.TotalRows); // Root count is distinct from graph node budget.
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task InvalidOrExcessDepthEdgesRejectWithoutPartialResults(int fault)
    {
        var root = Graph();
        var options = CosmicQueryGraphOptions.Create(maxChildDepth: fault == 5 ? 1 : 0).Result;
        switch (fault)
        {
            case 0: root.Children[0].Children.Add(root); break;
            case 1: root.Children.Add(root.Children[0]); break;
            case 2: root.Children[0].ParentHolonId = Guid.NewGuid(); break;
            case 3: root.Children[0].Id = Guid.Empty; break;
            case 4: root.Children.Add(null!); break;
        }
        var adapter = new Adapter(root);
        var result = await CosmicQueryExecutor.ExecuteAsync(Plan(options, adapter), new[] { adapter });

        Assert.True(result.IsError);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NodeAndUtf8PayloadBudgetsCoverWholeGraphs(bool payload)
    {
        var root = Graph();
        root.Children[0].Description = new string('\u20ac', 1024);
        var options = CosmicQueryGraphOptions.Create(maximumNodes: payload ? 100 : 2,
            maximumPayloadBytes: payload ? 2048 : 1024 * 1024).Result;
        var adapter = new Adapter(root);
        var result = await CosmicQueryExecutor.ExecuteAsync(Plan(options, adapter), new[] { adapter });

        Assert.True(result.IsError);
        Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.Equal("child", root.Children[0].Name);
    }

    [Fact]
    public async Task GraphBudgetIsSharedAcrossProviderGroups()
    {
        var first = Graph(); first.Children[0].Children.Clear();
        var second = Graph(); second.Children[0].Children.Clear();
        var adapters = new[] { new Adapter(first), new Adapter(second, ProviderType.MongoDBOASIS) };
        var options = CosmicQueryGraphOptions.Create(maximumNodes: 3).Result;
        var result = await CosmicQueryExecutor.ExecuteAsync(Plan(options, adapters), adapters);

        Assert.True(result.IsError);
        Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task DeepGraphsUseHeapTraversal()
    {
        var root = new Holon { Id = Guid.NewGuid() };
        var current = root;
        for (var index = 0; index < 1200; index++)
        {
            var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = current.Id };
            current.Children.Add(child);
            current = child;
        }
        var adapter = new Adapter(root);
        var result = await CosmicQueryExecutor.ExecuteAsync(
            Plan(CosmicQueryGraphOptions.Create(maximumNodes: 1201).Result, adapter), new[] { adapter });

        Assert.False(result.IsError, result.Message + result.Exception);
        var copy = Assert.Single(Assert.Single(result.Result.Providers).Rows);
        var depth = 0;
        while (copy.Children.Count != 0) { copy = Assert.Single(copy.Children); depth++; }
        Assert.Equal(1200, depth);
    }

    [Fact]
    public async Task TypedGraphLoadsAreCleanAndChildEditsAreTracked()
    {
        var source = Graph();
        var adapter = new Adapter(source);
        var result = await Manager().ExecuteQueryAsync(Plan(CosmicQueryGraphOptions.Create().Result, adapter), new[] { adapter });

        Assert.False(result.IsError, result.Message + result.Exception);
        var root = Assert.Single(Assert.Single(result.Result.Providers).Rows);
        var child = Assert.Single(root.Children);
        Assert.False(HolonChangeTracking.HasChanges(root));
        Assert.False(HolonChangeTracking.HasChanges(child));
        child.Name = "changed locally";
        Assert.True(HolonChangeTracking.HasChanges(root));
        Assert.Equal("child", source.Children[0].Name);
    }

    [Fact]
    public async Task ATypeFilteredGraphCannotReplaceUnobservedMembership()
    {
        var source = Graph();
        var child = source.Children[0];
        child.HolonType = HolonType.Quest;
        child.Children.Clear();
        var hidden = Guid.NewGuid();
        source.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
            1, source.Id, new[] { new HolonChildRole("Children", new[] { child.Id, hidden }) }));
        var adapter = new Adapter(source);
        var options = CosmicQueryGraphOptions.Create(childType: HolonType.Quest).Result;
        var result = await Manager().ExecuteQueryAsync(Plan(options, adapter), new[] { adapter });

        Assert.False(result.IsError, result.Message + result.Exception);
        var root = Assert.Single(Assert.Single(result.Result.Providers).Rows);
        Assert.Single(root.Children);
        Assert.Contains(hidden.ToString("D"), root.ChildRoleManifestJson);
        Assert.False(HolonChangeTracking.HasChanges(root));
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var selected = HolonSavePlan.Create(new[] { root }, true, true, 0);
        });
        using var excluded = HolonSavePlan.Create(new[] { root }, false, false, 0);
        Assert.Empty(excluded.RelationshipManifests);
        Assert.Contains(hidden.ToString("D"), root.ChildRoleManifestJson);
    }

    [Fact]
    public async Task ADepthBoundaryRetainsItsUnloadedGrandchildMembership()
    {
        var source = Graph();
        var child = source.Children[0];
        var grandchild = child.Children[0].Id;
        source.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
            1, source.Id, new[] { new HolonChildRole("Children", new[] { child.Id }) }));
        ((Holon)child).ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
            1, child.Id, new[] { new HolonChildRole("Children", new[] { grandchild }) }));
        child.Children.Clear();
        var adapter = new Adapter(source);
        var result = await Manager().ExecuteQueryAsync(
            Plan(CosmicQueryGraphOptions.Create(maxChildDepth: 1).Result, adapter), new[] { adapter });

        Assert.False(result.IsError, result.Message + result.Exception);
        var root = Assert.Single(Assert.Single(result.Result.Providers).Rows);
        var loadedChild = (Holon)Assert.Single(root.Children);
        Assert.Empty(loadedChild.Children);
        Assert.Contains(grandchild.ToString("D"), loadedChild.ChildRoleManifestJson);
        using var selected = HolonSavePlan.Create(new[] { root }, true, true, 1);
        Assert.Contains(root.Id, selected.RelationshipManifests.Keys);
        Assert.DoesNotContain(loadedChild.Id, selected.RelationshipManifests.Keys);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AnIncompleteOrForeignManifestCannotBePublishedByTheExecutor(bool foreign)
    {
        var source = Graph();
        source.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
            1, foreign ? Guid.NewGuid() : source.Id,
            new[] { new HolonChildRole("Children", new[] { Guid.NewGuid() }) }));
        var adapter = new Adapter(source);
        var result = await CosmicQueryExecutor.ExecuteAsync(
            Plan(CosmicQueryGraphOptions.Create().Result, adapter), new[] { adapter });

        Assert.True(result.IsError);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task EncryptedChildrenDecryptBeforeTrackingOrRejectTheEntireGraph(bool partial, bool wrongKey)
    {
        var source = Graph();
        var child = source.Children[0];
        child.HolonType = HolonType.Quest;
        child.Children[0].HolonType = HolonType.Quest;
        var settings = new EncryptionSettings { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('e', 32) };
        var ciphertext = NextGenSoftware.OASIS.API.Core.Helpers.PasswordEncryptionHelper.EncryptValue("{\"secret\":\"child\"}", settings);
        child.MetaData["__oasis_enc__"] = ciphertext;
        var manager = Manager();
        if (wrongKey) settings.Rijndael256Key = new string('w', 32);
        manager.OASISDNA.OASIS.Security = new SecuritySettings { HolonDataEncryption = settings };
        var adapter = new Adapter(source);
        var options = CosmicQueryGraphOptions.Create(childType: partial ? HolonType.Quest : HolonType.All).Result;
        var result = await manager.ExecuteQueryAsync(Plan(options, adapter), new[] { adapter });

        if (wrongKey)
        {
            Assert.True(result.IsError);
            Assert.Null(result.Result);
            Assert.Equal("HOLON_METADATA_DECRYPTION_FAILED", result.ErrorCode);
        }
        else
        {
            Assert.False(result.IsError, result.Message + result.Exception);
            var root = Assert.Single(Assert.Single(result.Result.Providers).Rows);
            var loadedChild = Assert.Single(root.Children);
            Assert.False(loadedChild.MetaData.ContainsKey("__oasis_enc__"));
            Assert.Equal("child", loadedChild.MetaData["secret"].ToString());
            Assert.False(HolonChangeTracking.HasChanges(root));
        }
        Assert.Equal(ciphertext, child.MetaData["__oasis_enc__"]);
    }

    public sealed class CountingRoot : Holon
    {
        public static int Constructions;
        public CountingRoot() { Constructions++; }
    }

    public sealed class RevokingFactoryRoot : Holon
    {
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings()
            =>new[]{HolonChildRoleBinding.Create<Holon>("Children",
                children=>
                {
                    Children=children.Cast<IHolon>().ToList();
                    foreach(var child in Children){child.IsPublic=false;child.CreatedByAvatarId=Guid.Empty;}
                },source=>StoredRecordJson.Project<Holon>(source))};
    }

    [Fact]
    public async Task ATypedFactoryCannotPublishAForeignReplacement()
    {
        var source=Graph();source.IsPublic=true;
        source.Children[0].IsPublic=true;source.Children[0].Children.Clear();
        source.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
            1,source.Id,new[]{new HolonChildRole("Children",new[]{source.Children[0].Id,Guid.NewGuid()})}));
        var adapter=new Adapter(source);
        var query=BindGraphPolicy(CosmicQuery<RevokingFactoryRoot>.Create()
            .WithChildren(CosmicQueryGraphOptions.Create().Result).Result,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result);
        var plan=CosmicQueryPlanner.Plan(query,adapter.QueryProviderType,
            new[]{new CosmicQueryTarget(adapter.QueryProviderType,adapter.QueryCapabilities)});
        Assert.False(plan.IsError,plan.Message);
        var read=await Manager().ExecuteQueryAsync(plan.Result,new[]{adapter});
        Assert.True(read.IsError);
        Assert.Equal("CQL_VISIBILITY_VIOLATION",read.ErrorCode);
        Assert.Null(read.Result);
    }

    public sealed class TypedChild : Holon
    {
        [NextGenSoftware.OASIS.API.Core.CustomAttrbiutes.CustomOASISProperty]
        public string Secret { get; set; } = "";
    }

    public sealed class FactoryRoot : Holon
    {
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings()
            => new[] { HolonChildRoleBinding.Create<TypedChild>("Children",
                children => Children = children.Cast<IHolon>().ToList(),
                source =>
                {
                    if (source.MetaData.ContainsKey("__oasis_enc__"))
                        throw new InvalidOperationException("Role materialization must receive admitted clear metadata.");
                    var child = StoredRecordJson.Project<TypedChild>(source);
                    child.Secret = source.MetaData["secret"].ToString()!;
                    return child;
                }) };
    }

    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task ARegisteredChildFactoryReceivesDecryptedMetadataBeforeTracking(bool partial,bool accessBound)
    {
        var source = Graph();
        var child = source.Children[0];
        child.Children.Clear();
        if(partial)child.HolonType=HolonType.Quest;
        var policy=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        if(accessBound){source.IsPublic=true;child.CreatedByAvatarId=policy.ActorId;}
        source.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
            1, source.Id, new[] { new HolonChildRole("Children", partial?new[]{child.Id,Guid.NewGuid()}:new[] { child.Id }) }));
        var settings = new EncryptionSettings { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('e', 32) };
        child.MetaData["__oasis_enc__"] = NextGenSoftware.OASIS.API.Core.Helpers.PasswordEncryptionHelper.EncryptValue("{\"secret\":\"mapped child\"}", settings);
        var manager = Manager();
        manager.OASISDNA.OASIS.Security = new SecuritySettings { HolonDataEncryption = settings };
        var adapter = new Adapter(source);
        var query = CosmicQuery<FactoryRoot>.Create().WithChildren(CosmicQueryGraphOptions.Create(childType:partial&&!accessBound?HolonType.Quest:HolonType.All).Result).Result;
        if(accessBound)query=BindGraphPolicy(query,policy);
        var plan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        var result = await manager.ExecuteQueryAsync(plan.Result, new[] { adapter });

        Assert.False(result.IsError, result.Message + result.Exception);
        var root = Assert.Single(Assert.Single(result.Result.Providers).Rows);
        var loadedChild = Assert.IsType<TypedChild>(Assert.Single(root.Children));
        Assert.Equal("mapped child", loadedChild.Secret);
        Assert.False(HolonChangeTracking.HasChanges(root));
        loadedChild.Secret = "edited locally";
        Assert.True(HolonChangeTracking.HasChanges(root));
        Assert.True(child.MetaData.ContainsKey("__oasis_enc__"));
        if(accessBound)
        {
            root.AcceptChanges();
            Assert.Throws<InvalidOperationException>(()=>HolonSavePlan.Create(new[]{root},true,true,0));
            using var rootOnly=HolonSavePlan.Create(new[]{root},false,false,0);
        }
    }

    [Fact]
    public async Task ARejectedChildCannotRunATypedRootConstructor()
    {
        var source = Graph();
        source.Children[0].MetaData["__oasis_enc__"] = "invalid ciphertext";
        var manager = Manager();
        manager.OASISDNA.OASIS.Security = new SecuritySettings
        {
            HolonDataEncryption = new EncryptionSettings
            { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('e', 32) }
        };
        var adapter = new Adapter(source);
        var query = CosmicQuery<CountingRoot>.Create().WithChildren(CosmicQueryGraphOptions.Create().Result).Result;
        var plan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        CountingRoot.Constructions = 0;
        var result = await manager.ExecuteQueryAsync(plan.Result, new[] { adapter });

        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.Equal(0, CountingRoot.Constructions);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LegacyPartialMembershipCannotBecomeAReplacementThroughAcceptChanges(bool typed)
    {
        var source = Graph();
        source.Children[0].HolonType = HolonType.Quest;
        source.Children[0].Children[0].HolonType = HolonType.Quest;
        var adapter = new Adapter(source);
        var plan = Plan(CosmicQueryGraphOptions.Create(childType: HolonType.Quest).Result, adapter);
        Holon root;
        if (typed)
        {
            var result = await Manager().ExecuteQueryAsync(plan, new[] { adapter });
            Assert.False(result.IsError, result.Message);
            root = Assert.Single(Assert.Single(result.Result.Providers).Rows);
        }
        else
        {
            var result = await CosmicQueryExecutor.ExecuteAsync(plan, new[] { adapter });
            Assert.False(result.IsError, result.Message);
            root = (Holon)Assert.Single(Assert.Single(result.Result.Providers).Rows);
        }
        Assert.Null(root.ChildRoleManifestJson);
        root.AcceptChanges(); // Accepting values cannot invent complete relationship observation.
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var selected = HolonSavePlan.Create(new[] { root }, true, true, 0);
        });
        using var excluded = HolonSavePlan.Create(new[] { root }, false, false, 0);
        Assert.Empty(excluded.RelationshipManifests);
        root.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
            1, root.Id, new[] { new HolonChildRole("Children", root.Children.Select(child => child.Id)) }));
        root.AcceptChanges();
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var forged = HolonSavePlan.Create(new[] { root }, true, true, 0);
        });
    }

    private static HolonManager Manager()
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        return new HolonManager(null, dna, new ProviderManager(null, dna));
    }

    private static CosmicQueryPlan<Holon> Plan(CosmicQueryGraphOptions options, params Adapter[] adapters)
        => CosmicQueryPlanner.Plan(CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).WithChildren(options).Result,
            adapters[0].QueryProviderType, adapters.Select(adapter => new CosmicQueryTarget(adapter.QueryProviderType,
                adapter.QueryCapabilities)).ToArray()).Result;

    private sealed class Adapter : ICosmicQueryStorageProvider
    {
        private readonly IHolon _root;
        public Adapter(IHolon root, ProviderType provider = ProviderType.SQLLiteDBOASIS)
        { _root = root; QueryProviderType = provider; }

        public ProviderType QueryProviderType { get; }
        public CosmicQueryCapabilities QueryCapabilities => new(CosmicQueryFeatures.GraphExpansion|CosmicQueryFeatures.Or|CosmicQueryFeatures.And,
            new[]{new CosmicQueryFieldCapability("IsPublic",typeof(bool),CosmicQueryComparisonOperator.Equal),
                new CosmicQueryFieldCapability("CreatedByAvatarId",typeof(Guid),CosmicQueryComparisonOperator.Equal)});

        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(new[] { _root }));
    }
}
