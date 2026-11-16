extern alias ShippingWeb4;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using CqlCompiler = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.CqlV1Compiler;
using CqlJoinRequest = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlJoinV1Request;
using CqlJoinColumn = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlJoinV1Column;
using CqlJoinFilter = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Filter;
using CqlJoinKey = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlJoinV1Key;
using CqlJoinOrder = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1JoinOrder;
using CqlJoinOperator = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Operator;
using SqliteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyChildSource(ICosmicQueryJoinStorageProvider provider, Guid actor, Guid parentId, Guid childId,
        List<object> checks)
    {
        var scope = CosmicQueryScope.Named(provider.QueryProviderType).Result;
        var source = CosmicQuery<Holon>.Create(scope).WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var parents = source.Where(row => row.Id == parentId).Take(1).Result;
        var options = CosmicJoinOptions.Create(maximumLeftRows: 1, maximumRightRows: 1).Result;
        var joined = parents.JoinChildren(source, options).Select((parent, child) => child.Id);
        Assert.IsFalse(joined.IsError, joined.Message);
        var plan = CosmicJoinPlanner.Plan(joined.Result, provider.QueryProviderType,
            new[] { new CosmicJoinTarget(provider.QueryProviderType, provider.QueryCapabilities, provider.JoinCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var rows = await NativeManager(provider).ExecuteJoinQueryAsync(plan.Result, new[] { provider });
        Assert.IsFalse(rows.IsError, rows.Message + rows.Exception);
        Assert.AreEqual(childId, rows.Result.Providers.Single().Rows.Single().Value);
        var linq = CosmicLinq.Create(NativeManager(provider), provider, parents, maximumResults: 1).Result;
        var flattened = await linq.SelectMany(parent => parent.Children,
            (parent, child) => new { ParentId = parent.Id, ChildId = child.Id })
            .Select(pair => pair.ChildId).ToCosmicListAsync();
        Assert.IsFalse(flattened.IsError, flattened.Message + flattened.Exception);
        Assert.AreEqual(childId, flattened.Result.Single());
        var collectionProjection = linq.SelectMany(parent => parent.Children.Select(child => child.Id))
            .Select(id => new { Identity = id });
        var selectedChildren = await collectionProjection.ToCosmicListAsync();
        Assert.IsFalse(selectedChildren.IsError, selectedChildren.Message + selectedChildren.Exception);
        Assert.AreEqual(childId, selectedChildren.Result.Single().Identity);
        var childName = "visible match";
        var filtered = linq.SelectMany(parent => parent.Children.Where(child => child.Name == childName),
            (parent, child) => child.Id);
        CollectionAssert.AreEqual(new[] { childId }, (await filtered.ToCosmicListAsync()).Result.ToArray());
        childName = "absent";
        var noChildren = await filtered.ToCosmicListAsync();
        Assert.IsFalse(noChildren.IsError, noChildren.Message + noChildren.Exception);
        Assert.AreEqual(0, noChildren.Result.Count);
        var projectedFilter = linq.SelectMany(parent => parent.Children.Select(child => new { child.Id, child.Name }))
            .Where(child => child.Name == "visible match").Select(child => child.Id);
        var filteredProjection = await projectedFilter.ToCosmicListAsync();
        Assert.IsFalse(filteredProjection.IsError, filteredProjection.Message + filteredProjection.Exception);
        Assert.AreEqual(childId, filteredProjection.Result.Single());
        var request = new CqlJoinRequest
        {
            Kind = "Child",
            OutputOrdering = new[] { new CqlJoinOrder { Alias = "ChildId", Descending = true },
                new CqlJoinOrder { Alias = "ChildName" } },
            Left = new() { Take = 1, Filters = new[] {
                new CqlJoinFilter { Field = "Id", Operator = CqlJoinOperator.Equal,
                    Value = JsonSerializer.SerializeToElement(parentId) } } },
            MaximumLeftRows = 1, MaximumRightRows = 1,
            Columns = new[] { new CqlJoinColumn { Alias = "ChildId", Source = "Right", Selection = new() { Field = "Id" } },
                new CqlJoinColumn { Alias = "ChildName", Source = "Right", Selection = new() { Field = "Name" } } }
        };
        var wire = CqlCompiler.CompileJoin(request, actor, provider.QueryProviderType, CosmicQueryReadAccess.OwnerOrPublic(actor).Result);
        Assert.IsTrue(wire.Join.RestrictsRightToLeftParents);
        Assert.AreEqual(0, wire.OutputOrderColumn);
        Assert.IsTrue(wire.OutputOrderDescending);
        Assert.AreEqual(new CosmicJoinOutputOrder(1, false), wire.OutputOrdering[1]);
        var wirePlan = CosmicJoinPlanner.Plan(wire, provider.QueryProviderType,
            new[] { new CosmicJoinTarget(provider.QueryProviderType, provider.QueryCapabilities, provider.JoinCapabilities) });
        Assert.IsFalse(wirePlan.IsError, wirePlan.Message);
        var wireRows = await NativeManager(provider).ExecuteJoinQueryAsync(wirePlan.Result, new[] { provider });
        Assert.IsFalse(wireRows.IsError, wireRows.Message + wireRows.Exception);
        Assert.AreEqual(childId, (Guid)wireRows.Result.Providers.Single().Rows.Single().Value["ChildId"]);
        request.OutputOffset = 1;
        request.OutputLimit = 1;
        var page = CqlCompiler.CompileJoin(request, actor, provider.QueryProviderType, CosmicQueryReadAccess.OwnerOrPublic(actor).Result);
        Assert.AreEqual(1, page.OutputOffset);
        Assert.AreEqual(1, page.OutputLimit);
        Assert.AreEqual(2, page.OutputOrdering.Count);
        var pagePlan = CosmicJoinPlanner.Plan(page, provider.QueryProviderType,
            new[] { new CosmicJoinTarget(provider.QueryProviderType, provider.QueryCapabilities, provider.JoinCapabilities) });
        Assert.IsFalse(pagePlan.IsError, pagePlan.Message);
        var pageRows = await NativeManager(provider).ExecuteJoinQueryAsync(pagePlan.Result, new[] { provider });
        Assert.IsFalse(pageRows.IsError, pageRows.Message + pageRows.Exception);
        Assert.AreEqual(0, pageRows.Result.Providers.Single().Rows.Count);
        request.OutputLimit = null;
        Assert.ThrowsException<ArgumentException>(() => CqlCompiler.CompileJoin(request, actor,
            provider.QueryProviderType, CosmicQueryReadAccess.OwnerOrPublic(actor).Result));
        checks.Add(new { provider = provider.QueryProviderType.ToString(), linq = "scoped child input before native budget" });
    }

    private static async Task VerifyJoinOutputOrdering(ICosmicQueryJoinStorageProvider provider, Guid[] ids, List<object> checks)
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider.QueryProviderType).Result);
        var admitted = new[] { ids[0], ids[1], ids[4] };
        var parentId = ids[0];
        var left = source.Where(row => row.Id == parentId).Result;
        var right = source.Where(row => admitted.Contains(row.Id)).OrderBy(row => row.Name).Result;
        var selected = left.CrossJoin(right).Select((parent, child) => new { Name = child.Name }).Result
            .OrderByOutput("Name", descending: true);
        Assert.IsFalse(selected.IsError, selected.Message);
        var native = provider;
        var sorted = CosmicJoinPlanner.Plan(selected.Result, native.QueryProviderType,
            new[] { new CosmicJoinTarget(native.QueryProviderType, native.QueryCapabilities, native.JoinCapabilities) });
        Assert.IsFalse(sorted.IsError, sorted.Message);
        var ordered = await NativeManager(provider).ExecuteJoinQueryAsync(sorted.Result, new[] { native });
        Assert.IsFalse(ordered.IsError, ordered.Message + ordered.Exception);
        CollectionAssert.AreEqual(new[] { "visible match", "protected source root", "grandchild two" },
            ordered.Result.Providers.Single().Rows.Select(row => row.Value.Name).ToArray());
        var byDescription = left.CrossJoin(right).Select((parent, child) => new { child.Id, child.Description }).Result;
        foreach (var descending in new[] { false, true })
        {
            var orderedDescriptions = byDescription.OrderByOutput("Description", descending);
            Assert.IsFalse(orderedDescriptions.IsError, orderedDescriptions.Message);
            var nullablePlan = CosmicJoinPlanner.Plan(orderedDescriptions.Result, native.QueryProviderType,
                new[] { new CosmicJoinTarget(native.QueryProviderType, native.QueryCapabilities, native.JoinCapabilities) });
            Assert.IsFalse(nullablePlan.IsError, nullablePlan.Message);
            var nullableRows = await NativeManager(provider).ExecuteJoinQueryAsync(nullablePlan.Result, new[] { native });
            Assert.IsFalse(nullableRows.IsError, nullableRows.Message + nullableRows.Exception);
            var expected = descending ? new[] { ids[0], ids[1], ids[4] } : new[] { ids[4], ids[0], ids[1] };
            CollectionAssert.AreEqual(expected, nullableRows.Result.Providers.Single().Rows.Select(row => row.Value.Id).ToArray());
        }
        var composite = left.CrossJoin(right).Select((parent, child) => new { child.Id, child.Description, child.Name }).Result
            .OrderByOutput("Description").Result.ThenByOutput("Name", descending: true);
        Assert.IsFalse(composite.IsError, composite.Message);
        var compositePlan = CosmicJoinPlanner.Plan(composite.Result, native.QueryProviderType,
            new[] { new CosmicJoinTarget(native.QueryProviderType, native.QueryCapabilities, native.JoinCapabilities) });
        Assert.IsFalse(compositePlan.IsError, compositePlan.Message);
        var compositeRows = await NativeManager(provider).ExecuteJoinQueryAsync(compositePlan.Result, new[] { native });
        Assert.IsFalse(compositeRows.IsError, compositeRows.Message + compositeRows.Exception);
        CollectionAssert.AreEqual(new[] { ids[4], ids[1], ids[0] },
            compositeRows.Result.Providers.Single().Rows.Select(row => row.Value.Id).ToArray());
        foreach (var (offset, limit, expected) in new[]
        {
            (1, 1, new[] { ids[1] }),
            (2, 4, new[] { ids[0] }),
            (3, 1, Array.Empty<Guid>()),
            (0, 0, Array.Empty<Guid>())
        })
        {
            var page = composite.Result.PageOutput(offset, limit);
            Assert.IsFalse(page.IsError, page.Message);
            var pagedPlan = CosmicJoinPlanner.Plan(page.Result, native.QueryProviderType,
                new[] { new CosmicJoinTarget(native.QueryProviderType, native.QueryCapabilities, native.JoinCapabilities) });
            Assert.IsFalse(pagedPlan.IsError, pagedPlan.Message);
            var pageRows = await NativeManager(provider).ExecuteJoinQueryAsync(pagedPlan.Result, new[] { native });
            Assert.IsFalse(pageRows.IsError, pageRows.Message + pageRows.Exception);
            CollectionAssert.AreEqual(expected, pageRows.Result.Providers.Single().Rows.Select(row => row.Value.Id).ToArray());
        }
        Assert.IsTrue(composite.Result.PageOutput(-1, 1).IsError);
        Assert.IsTrue(composite.Result.PageOutput(0, -1).IsError);
        var manager = NativeManager(provider);
        var leftRows = CosmicLinq.Create(manager, provider, left, maximumResults: 4).Result;
        var rightRows = CosmicLinq.Create(manager, provider, right, maximumResults: 4).Result;
        var linq = leftRows.SelectMany(_ => rightRows, (parent, child) => new { child.Name })
            .OrderByDescending(child => child.Name);
        var orderedLinq = await linq.ToCosmicListAsync();
        Assert.IsFalse(orderedLinq.IsError, orderedLinq.Message + orderedLinq.Exception);
        CollectionAssert.AreEqual(new[] { "visible match", "protected source root", "grandchild two" },
            orderedLinq.Result.Select(row => row.Name).ToArray());
        var scalar = leftRows.SelectMany(_ => rightRows, (parent, child) => child.Name).OrderBy(name => name);
        var orderedScalar = await scalar.ToCosmicListAsync();
        Assert.IsFalse(orderedScalar.IsError, orderedScalar.Message + orderedScalar.Exception);
        CollectionAssert.AreEqual(new[] { "grandchild two", "protected source root", "visible match" },
            orderedScalar.Result.ToArray());
        var thenBy = leftRows.SelectMany(_ => rightRows, (parent, child) => new { child.Id, child.Name, child.Description })
            .OrderBy(child => child.Description).ThenByDescending(child => child.Name);
        var orderedThenBy = await thenBy.ToCosmicListAsync();
        Assert.IsFalse(orderedThenBy.IsError, orderedThenBy.Message + orderedThenBy.Exception);
        CollectionAssert.AreEqual(new[] { ids[4], ids[1], ids[0] }, orderedThenBy.Result.Select(row => row.Id).ToArray());
        foreach (var (query, expected) in new[]
        {
            (thenBy.Skip(1).Take(1), new[] { ids[1] }),
            (thenBy.Skip(1), new[] { ids[1], ids[0] }),
            (thenBy.Take(2).Skip(1), new[] { ids[1] }),
            (thenBy.Take(1).Skip(2), Array.Empty<Guid>()),
            (thenBy.Skip(1).Skip(1).Take(4).Take(1), new[] { ids[0] }),
            (thenBy.Skip(-1).Take(1), new[] { ids[4] }),
            (thenBy.Take(-1), Array.Empty<Guid>())
        })
        {
            var paged = await query.ToCosmicListAsync();
            Assert.IsFalse(paged.IsError, paged.Message + paged.Exception);
            CollectionAssert.AreEqual(expected, paged.Result.Select(row => row.Id).ToArray());
        }
        var lowerId = ids[1];
        var missingId = ids[4];
        var leftKeys = new[] { parentId, lowerId };
        var rightKeys = new[] { parentId, missingId };
        var lower = source.Where(row => leftKeys.Contains(row.Id)).Result;
        var higher = source.Where(row => rightKeys.Contains(row.Id)).Result;
        var keys = new[] { CosmicProjectionSelection<Holon>.Create("Amount",
            row => CosmicQueryMetadata.Value<long?>(row, "i64")).Result };
        var full = lower.FullJoinColumns(higher, keys, keys).Result;
        var compared = full.CompareKey("Amount", CosmicJoinComparison.LessThan);
        Assert.IsFalse(compared.IsError, compared.Message);
        var picked = compared.Result.Select((l, r) => new { Left = (Guid?)l.Id, Right = (Guid?)r.Id });
        Assert.IsFalse(picked.IsError, picked.Message);
        var comparePlan = CosmicJoinPlanner.Plan(picked.Result, native.QueryProviderType,
            new[] { new CosmicJoinTarget(native.QueryProviderType, native.QueryCapabilities, native.JoinCapabilities) });
        Assert.IsFalse(comparePlan.IsError, comparePlan.Message);
        var compareRows = await manager.ExecuteJoinQueryAsync(comparePlan.Result, new[] { native });
        Assert.IsFalse(compareRows.IsError, compareRows.Message + compareRows.Exception);
        var comparedRows = compareRows.Result.Providers.Single().Rows;
        CollectionAssert.AreEquivalent(new (Guid?, Guid?)[] { (parentId, null), (lowerId, parentId), (null, missingId) },
            comparedRows.Select(row => (row.Value.Left, row.Value.Right)).ToArray());
        Assert.AreEqual(3, comparedRows.Count);
        var wireActor = Guid.NewGuid();
        var access = CosmicQueryReadAccess.OwnerOrPublic(wireActor).Result;
        var request = new CqlJoinRequest
        {
            Provider = native.QueryProviderType.ToString(),
            Kind = "Full",
            Left = new() { Take = 2, Filters = new[] { KeySet(leftKeys) } },
            Right = new() { Take = 2, Filters = new[] { KeySet(rightKeys) } },
            Keys = new[] { new CqlJoinKey
            {
                Alias = "Amount",
                Operator = "LessThan",
                Left = new() { MetadataPath = new[] { "i64" }, ValueType = "Int64", Nullable = true },
                Right = new() { MetadataPath = new[] { "i64" }, ValueType = "Int64", Nullable = true }
            } },
            Columns = new[]
            {
                new CqlJoinColumn { Alias = "LeftId", Source = "Left", AllowMissing = true, Selection = new() { Field = "Id" } },
                new CqlJoinColumn { Alias = "RightId", Source = "Right", AllowMissing = true, Selection = new() { Field = "Id" } }
            },
            MaximumLeftRows = 2, MaximumRightRows = 2, MaximumPairs = 4, MaximumResults = 4
        };
        var wire = CqlCompiler.CompileJoin(request, wireActor, native.QueryProviderType, access);
        Assert.AreEqual(CosmicJoinComparison.LessThan, wire.Join.Keys.Single().Comparison);
        var wirePlan = CosmicJoinPlanner.Plan(wire, native.QueryProviderType,
            new[] { new CosmicJoinTarget(native.QueryProviderType, native.QueryCapabilities, native.JoinCapabilities) });
        Assert.IsFalse(wirePlan.IsError, wirePlan.Message);
        var wireRows = await manager.ExecuteJoinQueryAsync(wirePlan.Result, new[] { native });
        Assert.IsFalse(wireRows.IsError, wireRows.Message + wireRows.Exception);
        CollectionAssert.AreEquivalent(new (Guid?, Guid?)[] { (parentId, null), (lowerId, parentId), (null, missingId) },
            wireRows.Result.Providers.Single().Rows.Select(row => ((Guid?)row.Value["LeftId"], (Guid?)row.Value["RightId"])).ToArray());
        request.Keys[0].Operator = "Invalid";
        Assert.ThrowsException<ArgumentException>(() => CqlCompiler.CompileJoin(request, wireActor, native.QueryProviderType, access));

        static CqlJoinFilter KeySet(Guid[] selectedIds) => new()
        {
            Field = "Id", Operator = CqlJoinOperator.In, Scalar = "Guid", Value = JsonSerializer.SerializeToElement(selectedIds)
        };
        var byVersion = leftRows.SelectMany(l => rightRows.Where(r => r.Version >= l.Version),
            (l, r) => new { r.Id, r.Name });
        var nativeVersionPage = await byVersion.OrderBy(r => r.Name).Skip(1).Take(1).ToCosmicListAsync();
        Assert.IsFalse(nativeVersionPage.IsError, nativeVersionPage.Message + nativeVersionPage.Exception);
        Assert.AreEqual(ids[0], nativeVersionPage.Result.Single().Id);
        var strictVersion = leftRows.SelectMany(l => rightRows.Where(r => l.Version < r.Version),
            (l, r) => r.Id);
        var noStrictMatches = await strictVersion.ToCosmicListAsync();
        Assert.IsFalse(noStrictMatches.IsError, noStrictMatches.Message + noStrictMatches.Exception);
        Assert.AreEqual(0, noStrictMatches.Result.Count);
        var wide = CosmicJoinOptions.Create(maximumLeftRows: CosmicJoinOptions.MaximumInputRows,
            maximumRightRows: CosmicJoinOptions.MaximumInputRows, maximumPairs: 1, maximumResults: 1).Result;
        var tooWide = lower.JoinColumns(higher, keys, keys, wide).Result.CompareKey("Amount", CosmicJoinComparison.LessThan)
            .Result.Select((l, r) => r.Id).Result;
        Assert.AreEqual("CQL_RESULT_LIMIT", native.JoinCapabilities.Admit(tooWide).ErrorCode);
        var limited = lower.Take(1).Result.JoinColumns(higher.Take(1).Result, keys, keys, wide).Result
            .CompareKey("Amount", CosmicJoinComparison.LessThan).Result.Select((l, r) => r.Id).Result;
        var limitedAdmission = native.JoinCapabilities.Admit(limited);
        Assert.IsFalse(limitedAdmission.IsError, limitedAdmission.Message);
        checks.Add(new { provider = provider.QueryProviderType.ToString(), join = "native selected output ordering" });
    }

    [TestMethod]
    public async Task NativeSqliteEntityProjectionPreservesStagesAndProofBeforeDtoConstruction()
    {
        var rootPath=Environment.GetEnvironmentVariable("OASIS_CQL_NEO_ENTITY_PROJECTION_ROOT")
            ?? throw new InvalidOperationException("Use the explicit entity-projection runner.");
        const string connection="Data Source=cosmic-entity-projection-linq;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper=new SqliteConnection(connection);await keeper.OpenAsync();
        using var provider=new SqliteProvider(connection){IsVersionControlEnabled=false};
        var activated=await provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);
        try
        {
            var ids=Enumerable.Range(0,5).Select(_=>Guid.NewGuid()).ToArray();
            var actor=Guid.NewGuid();
            var saved=await provider.SaveHolonAsync(CreateEntityProjectionFixture(ids,actor,Guid.NewGuid()),saveChildren:true);
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            var checks=new List<object>();
            await VerifyEntityProjection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyStandardEntityProjection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedComposition((ICosmicQueryEntityProjectionCompositionStorageProvider)provider,ids[0],ids,checks);
            await VerifyEntitySelectedReselection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedCardinality((ICosmicQueryEntityProjectionCardinalityStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedAll((ICosmicQueryEntityProjectedAllStorageProvider)provider,ids[0],checks);
            await VerifyMetadataPresenceWire(provider, ids[0], checks);
            await VerifyDistinctWire(provider, ids, checks);
            await VerifyNativeFlattening(provider,ids[0],ids[1],checks);
            var unlisted = await provider.SaveHolonAsync(new Holon
            {
                Id = Guid.NewGuid(), ParentHolonId = ids[0], Name = "visible outside the child manifest",
                CreatedByAvatarId = actor, IsPublic = true
            }, saveChildren: false);
            Assert.IsFalse(unlisted.IsError, unlisted.Message + unlisted.Exception);
            await VerifyChildSource(provider, actor, ids[0], ids[1], checks);
            await VerifyJoinOutputOrdering(provider, ids, checks);
            Assert.AreEqual(66,checks.Count);
            await File.WriteAllTextAsync(Path.Combine(rootPath,"TestResults/Neo4jCurrent/sqlite-entity-projection-native.json"),
                JsonSerializer.Serialize(new{provider="SQLLiteDBOASIS",checks=checks.Count,cases=checks,inMemoryFixture=true}));
        }
        finally{Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);}
    }
}
