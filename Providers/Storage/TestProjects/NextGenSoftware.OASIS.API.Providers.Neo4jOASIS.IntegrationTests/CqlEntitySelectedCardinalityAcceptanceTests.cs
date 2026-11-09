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
