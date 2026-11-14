using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyEntitySelectedCardinality(ICosmicQueryEntityProjectionCardinalityStorageProvider adapter,
        Guid rootId,List<object> checks)
    {
        var scope=CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        var source=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId || row.ParentHolonId==rootId).Result;
        var root=CosmicLinq.Create(NativeManager(adapter),adapter,source).Result;
        void Passed(string label)=>checks.Add(new{provider=adapter.QueryProviderType.ToString(),entitySelectedCardinality=label});
        var selected=root.OrderBy(row=>row.Name).Take(3).Reverse().Take(2).Select(row=>row.Name);
        Assert.AreEqual(2,selected.Count());Assert.AreEqual(2L,selected.LongCount());Assert.IsTrue(selected.Any());
        Passed("native Count LongCount Any after entity windows without selected stages");

        var expected=selected.ToArray();
        Assert.AreEqual(2,expected.Length);
        Assert.AreEqual(expected[0],selected.First());
        Assert.AreEqual(expected[1],selected.Last());
        Assert.AreEqual(expected[1],selected.ElementAt(1));
        Assert.AreEqual(expected[1],selected.ElementAt(^1));
        Assert.AreEqual(expected[0],selected.Take(1).Single());
        Assert.AreEqual("missing",selected.Take(0).FirstOrDefault("missing"));
        Passed("native entity-selected element terminals retain window order and explicit default");

        var versions = root.OrderBy(row => row.Name).Take(3).Reverse().Take(2)
            .Select(row => new { row.Version, row.Name }).Where(row => row.Version >= 1);
        var expectedVersions = versions.ToArray();
        Assert.IsTrue(expectedVersions.Length > 0);
        Assert.AreEqual(expectedVersions.Min(row => row.Version), versions.Min(row => row.Version));
        Assert.AreEqual(expectedVersions.Max(row => row.Version), versions.Select(row => row.Version).Max());
        Assert.AreEqual(expectedVersions.Sum(row => row.Version), versions.Sum(row => row.Version));
        Assert.AreEqual(expectedVersions.Average(row => row.Version), versions.Select(row => row.Version).Average());
        var amounts = root.Where(row => row.Id == rootId).OrderBy(row => row.Name).Take(1)
            .Reverse().Select(row => CosmicQueryMetadata.Value<decimal>(row, "decimal"));
        const decimal exactAmount = 1.0000000000000000000000000001m;
        Assert.AreEqual(exactAmount, amounts.Sum());
        Assert.AreEqual(exactAmount, amounts.Average());
        Assert.AreEqual(0m, amounts.Take(0).Sum());
        var longValues = root.Where(row => row.Id == rootId).OrderBy(row => row.Name).Take(1)
            .Reverse().Select(row => CosmicQueryMetadata.Value<long?>(row, "i64"));
        const long exactLong = 9007199254740993L;
        Assert.AreEqual<long?>(exactLong, longValues.Sum());
        Assert.AreEqual<double?>((double)exactLong, longValues.Average());
        Assert.AreEqual<long?>(0L, longValues.Take(0).Sum());
        Assert.IsNull(longValues.Take(0).Average());

        var floatingRoot = root.Where(row => row.Id == rootId).OrderBy(row => row.Name).Take(1).Reverse();
        var singles = floatingRoot.Select(row => CosmicQueryMetadata.Value<float?>(row, "float"));
        const float expectedSingle = 1f + 1.1920929e-7f;
        Assert.AreEqual<float?>(expectedSingle, singles.Sum());
        Assert.AreEqual<float?>(expectedSingle, singles.Average());
        Assert.AreEqual<float?>(0f, singles.Take(0).Sum());
        Assert.IsNull(singles.Take(0).Average());

        var doubles = floatingRoot.Select(row => CosmicQueryMetadata.Value<double>(row, "double"));
        const double expectedDouble = 1d + 2.220446049250313e-16d;
        Assert.AreEqual(expectedDouble, doubles.Sum());
        Assert.AreEqual(expectedDouble, doubles.Average());
        Assert.AreEqual(0d, doubles.Take(0).Sum());
        var entityWindow = source.Where(row => row.Id == rootId).Result.Compose().Result.Take(1).Result;
        var unsigned = entityWindow.Select(row => CosmicQueryMetadata.Value<ulong>(row, "u64")).Result;
        var target = new CosmicProjectionCompositionTarget(adapter.QueryProviderType, adapter.QueryCapabilities,
            adapter.ProjectionCapabilities, adapter.CompositionCapabilities);
        async Task<TValue> ReduceUnsigned<TValue>(CosmicScalarOperation operation)
        {
            var plan = CosmicProjectedNumericPlanner.PlanEntity<Holon, ulong, TValue>(unsigned, 0, operation,
                adapter.QueryProviderType, target, adapter.EntityCompositionCapabilities);
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await NativeManager(adapter).ExecuteProjectedNumericQueryAsync(plan.Result,
                new ICosmicQueryStorageProvider[] { adapter });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.IsTrue(result.IsLoaded);
            return result.Result.Providers.Single().Value;
        }
        Assert.AreEqual(ulong.MaxValue, await ReduceUnsigned<ulong>(CosmicScalarOperation.Sum));
        Assert.AreEqual((decimal)ulong.MaxValue, await ReduceUnsigned<decimal>(CosmicScalarOperation.Average));
        Passed("native integer, decimal, floating-point and exact UInt64 reductions preserve entity windows");

        var nullable = root.OrderBy(row => row.Name).Take(3).Reverse().Take(0)
            .Select(row => CosmicQueryMetadata.Value<int?>(row, "nullableInt"));
        Assert.IsNull(nullable.Min());
        Passed("native Min and Max preserve entity windows, selected predicates, reselection and empty nullable output");

        var first=root.OrderBy(row=>row.Name).Take(1).Where(row=>row.Version>=1)
            .Select(row=>new NativeProjectionProbe{Name=row.Name}).Select(row=>row.Name).Take(1);
        NativeProjectionProbe.Constructors=0;
        Assert.AreEqual(0,first.Count(name=>name=="protected source root"));
        Assert.AreEqual(0L,first.LongCount(name=>name=="protected source root"));
        Assert.IsFalse(first.Any(name=>name=="protected source root"));
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Passed("terminal predicate remains after original and selected windows across reselection");

        var entity=CosmicQuery<NativeCountWithoutConstructor>.Create(scope).Where(row=>row.Id==rootId).Take(1).Result.Compose().Result.Skip(0).Result;
        var query=entity.Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"i64")}).Result;
        async Task<NextGenSoftware.OASIS.Common.OASISResult<CosmicScalarExecution<TValue>>> Read<TValue>(
            CosmicEntityProjectionQuery<NativeCountWithoutConstructor,NativeProjectionProbe> captured,CosmicScalarOperation operation,CosmicEntityCompositionOptions? options=null)
        {
            var plan=CosmicProjectedCardinalityPlanner.PlanEntity<NativeCountWithoutConstructor,NativeProjectionProbe,TValue>(captured,operation,
                adapter.QueryProviderType,new CosmicProjectionCompositionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,adapter.ProjectionCapabilities,adapter.CompositionCapabilities),
                adapter.EntityCompositionCapabilities,adapter.SupportsEntityProjectionCardinality,options);
            Assert.IsFalse(plan.IsError,plan.Message);
            return await NativeManager(adapter).ExecuteProjectedCardinalityQueryAsync(plan.Result,new ICosmicQueryStorageProvider[]{adapter});
        }
        var count=await Read<int>(query,CosmicScalarOperation.Count);Assert.IsFalse(count.IsError,count.Message+count.Exception);
        Assert.AreEqual(1,count.Result.Providers.Single().Value);Assert.AreEqual(0,NativeProjectionProbe.Constructors);
        Passed("explicit cardinality retains typed input proof without source or DTO constructors");
        var empty=query.Take(0).Result;
        var emptyCount=await Read<int>(empty,CosmicScalarOperation.Count);Assert.IsFalse(emptyCount.IsError,emptyCount.Message+emptyCount.Exception);
        Assert.AreEqual(0,emptyCount.Result.Providers.Single().Value);
        var emptyAny=await Read<bool>(empty,CosmicScalarOperation.Any);Assert.IsFalse(emptyAny.IsError,emptyAny.Message+emptyAny.Exception);
        Assert.IsFalse(emptyAny.Result.Providers.Single().Value);Passed("empty selected cardinality yields zero and false");

        var typed=CosmicLinq.Create(NativeManager(adapter),adapter,CosmicQuery<NativeCountWithoutConstructor>.Create(scope).Where(row=>row.Id==rootId).Result).Result;
        var invalid=typed.Take(1).Where(row=>row.Version>=1)
            .Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"text")}).Select(row=>row.Name);
        Assert.AreEqual("CQL_DATA_INVALID",Assert.ThrowsException<CosmicLinqException>(()=>invalid.Take(0).Count()).Code);
        Assert.AreEqual("CQL_DATA_INVALID",Assert.ThrowsException<CosmicLinqException>(()=>invalid.Any()).Code);
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Passed("Count and Any cannot hide dropped invalid cells even behind empty output");
        var tight=CosmicQueryTraversalOptions.Create(2,1).Result;
        var protectedQuery=typed.Take(1).Where(row=>row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight))
            .Take(0).Select(row=>row.Name);
        Assert.AreEqual("CQL_QUERY_LIMIT",Assert.ThrowsException<CosmicLinqException>(()=>protectedQuery.Count()).Code);
        Passed("protected entity traversal error precedes cardinality");
        foreach(var options in new[]{new CosmicEntityCompositionOptions(maximumInputPayloadBytes:1),new CosmicEntityCompositionOptions(maximumStageRows:1)})
        {
            var bounded=entity.Take(0).Result.Select(row=>new NativeProjectionProbe{Name=row.Name}).Result.Take(0).Result;
            var limited=await Read<int>(bounded,CosmicScalarOperation.Count,options);
            Assert.AreEqual("CQL_QUERY_LIMIT",limited.ErrorCode,limited.Message+limited.Exception);Passed("entity source work budget precedes empty native count");
        }
    }
}
