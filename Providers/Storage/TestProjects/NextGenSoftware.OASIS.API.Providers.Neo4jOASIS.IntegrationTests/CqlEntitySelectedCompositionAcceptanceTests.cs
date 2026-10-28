using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyEntitySelectedComposition(ICosmicQueryEntityProjectionCompositionStorageProvider adapter,
        Guid rootId,Guid[] fixtureIds,List<object> checks)
    {
        async Task<NextGenSoftware.OASIS.Common.OASISResult<CosmicProjectionExecution<TResult>>> Read<T,TResult>(
            CosmicEntityProjectionQuery<T,TResult> query,CosmicEntityCompositionOptions? options=null) where T:IHolon
        {
            var plan=CosmicProjectionCompositionPlanner.PlanEntity(query,adapter.QueryProviderType,
                new CosmicProjectionCompositionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,adapter.ProjectionCapabilities,adapter.CompositionCapabilities),
                adapter.EntityCompositionCapabilities,options);
            Assert.IsFalse(plan.IsError,plan.Message);
            return await NativeManager(adapter).ExecuteProjectionCompositionQueryAsync(plan.Result,new ICosmicQueryStorageProvider[]{adapter});
        }
        void Passed(string label)=>checks.Add(new{provider=adapter.QueryProviderType.ToString(),entitySelectedComposition=label});
        var scope=CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        var original=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId || row.ParentHolonId==rootId).Result;
        var source=original.OrderBy(row=>row.Name).Take(3).Result.Compose().Result.Reverse().Result.Take(2).Result;
        var selected=source.Select(row=>row.Name).Result.OrderBy(name=>name).Result.Take(1).Result;
        var first=await Read(selected);Assert.IsFalse(first.IsError,first.Message+first.Exception);
        Assert.AreEqual("protected source root",first.Result.Providers.Single().Rows.Single());Passed("selected ordering follows the final entity page");

        var ties=original.OrderBy(row=>row.Name).Take(3).Result.Compose().Result
            .OrderBy(row=>row.Version).Result.Reverse().Result.Select(row=>row.Id).Result.Take(1).Result;
        var ordered=await Read(ties);Assert.IsFalse(ordered.IsError,ordered.Message+ordered.Exception);
        Assert.AreEqual(fixtureIds.Take(3).OrderByDescending(id=>id).First(),ordered.Result.Providers.Single().Rows.Single());
        Passed("final entity field ordering and reversed identity ties govern selected ranks");

        var root=CosmicLinq.Create(NativeManager(adapter),adapter,original).Result;
        var offset=await root.OrderBy(row=>row.Name).Skip(1).Take(2).Reverse().Select(row=>row.Name).Take(1).ToCosmicListAsync();
        Assert.IsFalse(offset.IsError,offset.Message+offset.Exception);
        Assert.AreEqual("visible match",offset.Result.Single());Passed("standard selected window does not replay original offset");
        var empty=await root.OrderBy(row=>row.Name).Take(1).Where(row=>row.Version>=1)
            .Select(row=>row.Name).Where(name=>name=="protected source root").ToCosmicListAsync();
        Assert.IsFalse(empty.IsError,empty.Message+empty.Exception);Assert.AreEqual(0,empty.Result.Count);Passed("selected filter cannot move before source page");

        var typedRoot=CosmicLinq.Create(NativeManager(adapter),adapter,
            CosmicQuery<NativeCountWithoutConstructor>.Create(scope).Where(row=>row.Id==rootId).Result).Result;
        long threshold=9007199254740992L;
        NativeProjectionProbe.Constructors=0;
        var typed=typedRoot.Take(1).Where(row=>row.Version>=1)
            .Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"i64"),
                Unsigned=CosmicQueryMetadata.Value<ulong>(row,"u64"),Amount=CosmicQueryMetadata.Value<decimal>(row,"decimal")})
            .Where(row=>row.Precision>threshold).OrderBy(row=>row.Name).ThenByDescending(row=>row.Precision).Reverse().Take(1);
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);
        var precision=await typed.ToCosmicListAsync();Assert.IsFalse(precision.IsError,precision.Message+precision.Exception);
        var dto=precision.Result.Single();Assert.AreEqual(9007199254740993L,dto.Precision);
        Assert.AreEqual(ulong.MaxValue,dto.Unsigned);Assert.AreEqual(1.0000000000000000000000000001m,dto.Amount);
        threshold=9007199254740993L;Assert.AreEqual(0,typed.ToArray().Length);
        Assert.AreEqual(1,NativeProjectionProbe.Constructors);Passed("exact native selected cells and rebinding without source constructors");

        NativeProjectionProbe.Constructors=0;
        var invalid=await typedRoot.Take(1).Where(row=>row.Version>=1)
            .Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"text")})
            .Take(0).ToCosmicListAsync();
        Assert.AreEqual("CQL_DATA_INVALID",invalid.ErrorCode,invalid.Message+invalid.Exception);
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Passed("invalid selected cells precede empty selected windows and DTOs");

        var entity=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId).Take(1).Result.Compose().Result;
        var tight=CosmicQueryTraversalOptions.Create(2,1).Result;
        var protectedQuery=entity.Where(row=>row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight)).Result
            .Take(0).Result.Select(row=>row.Name).Result.Take(0).Result;
        var failed=await Read(protectedQuery);Assert.AreEqual("CQL_QUERY_LIMIT",failed.ErrorCode,failed.Message+failed.Exception);
        Passed("entity protected scopes precede empty selected output");
        foreach(var options in new[]{new CosmicEntityCompositionOptions(maximumInputPayloadBytes:1),new CosmicEntityCompositionOptions(maximumStageRows:1)})
        {
            var bounded=entity.Skip(0).Result.Take(0).Result.Select(row=>row.Name).Result.Take(0).Result;
            var limited=await Read(bounded,options);Assert.AreEqual("CQL_QUERY_LIMIT",limited.ErrorCode,limited.Message+limited.Exception);
            Passed("entity source/work budget before selected empty output");
        }
    }
}
