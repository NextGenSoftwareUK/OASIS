using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyEntitySelectedAll(ICosmicQueryEntityProjectedAllStorageProvider adapter,Guid rootId,List<object> checks)
    {
        var scope=CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        var source=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId || row.ParentHolonId==rootId).Result;
        var root=CosmicLinq.Create(NativeManager(adapter),adapter,source).Result;
        void Passed(string label)=>checks.Add(new{provider=adapter.QueryProviderType.ToString(),entitySelectedAll=label});
        var selected=root.OrderBy(row=>row.Name).Take(3).Reverse().Take(2).Select(row=>row.Name);
        Assert.IsTrue(selected.All(name=>name!="private branch"));
        Assert.IsFalse(selected.All(name=>name=="visible match"));Passed("native All true and false after entity stages without selected stages");
        NativeProjectionProbe.Constructors=0;
        var first=root.OrderBy(row=>row.Name).Take(1).Where(row=>row.Version>=1)
            .Select(row=>new NativeProjectionProbe{Name=row.Name}).Select(row=>row.Name).Take(1);
        Assert.IsTrue(first.All(name=>name=="private branch"));Assert.IsFalse(first.All(name=>name=="protected source root"));
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Passed("All predicate remains after source and selected windows across reselection");

        var typed=CosmicLinq.Create(NativeManager(adapter),adapter,CosmicQuery<NativeCountWithoutConstructor>.Create(scope).Where(row=>row.Id==rootId).Result).Result;
        long threshold=9007199254740992L;
        var precision=typed.Take(1).Where(row=>row.Version>=1)
            .Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"i64")}).Take(1).Select(row=>row.Precision);
        Assert.IsTrue(precision.All(value=>value>threshold));threshold=9007199254740993L;
        Assert.IsFalse(precision.All(value=>value>threshold));Assert.AreEqual(0,NativeProjectionProbe.Constructors);
        Passed("exact Int64 All rebinding without source or intermediate DTO constructors");
        Assert.IsTrue(first.Take(0).All(name=>name=="does not match"));
        Assert.IsTrue(typed.Take(1).Where(row=>row.Version>=1).Take(0).Select(row=>row.Name).All(name=>name=="does not match"));
        Passed("empty entity and selected sequences are vacuously true after validation");

        var invalid=typed.Take(1).Where(row=>row.Version>=1)
            .Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"text")}).Select(row=>row.Name);
        Assert.AreEqual("CQL_DATA_INVALID",Assert.ThrowsException<CosmicLinqException>(()=>invalid.Take(0).All(name=>name=="anything")).Code);
        Assert.AreEqual("CQL_DATA_INVALID",Assert.ThrowsException<CosmicLinqException>(()=>invalid.All(name=>name=="does not match")).Code);
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Passed("All cannot hide dropped invalid cells behind true or false business outcomes");
        var tight=CosmicQueryTraversalOptions.Create(2,1).Result;
        var protectedQuery=typed.Take(1).Where(row=>row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight))
            .Take(0).Select(row=>row.Name);
        Assert.AreEqual("CQL_QUERY_LIMIT",Assert.ThrowsException<CosmicLinqException>(()=>protectedQuery.All(name=>name=="anything")).Code);
        Passed("protected entity traversal error precedes vacuous All");

        var entity=CosmicQuery<NativeCountWithoutConstructor>.Create(scope).Where(row=>row.Id==rootId).Take(1).Result.Compose().Result.Skip(0).Result;
        async Task<NextGenSoftware.OASIS.Common.OASISResult<CosmicScalarExecution<bool>>> Read(
            CosmicEntityProjectionQuery<NativeCountWithoutConstructor,NativeProjectionProbe> captured,Expression<Func<NativeProjectionProbe,bool>> predicate,CosmicEntityCompositionOptions? options=null)
        {
            var plan=CosmicProjectedAllPlanner.PlanEntity(captured,predicate,adapter.QueryProviderType,
                new CosmicProjectionCompositionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,adapter.ProjectionCapabilities,adapter.CompositionCapabilities),
                adapter.EntityCompositionCapabilities,adapter.SupportsEntityProjectedAll,options);
            Assert.IsFalse(plan.IsError,plan.Message);
            return await NativeManager(adapter).ExecuteProjectedAllQueryAsync(plan.Result,new ICosmicQueryStorageProvider[]{adapter});
        }
        var captured=entity.Select(row=>new NativeProjectionProbe{Name=row.Name}).Result;
        var explicitRead=await Read(captured,row=>row.Name=="protected source root");
        Assert.IsFalse(explicitRead.IsError,explicitRead.Message+explicitRead.Exception);Assert.IsTrue(explicitRead.Result.Providers.Single().Value);
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Passed("explicit SDK All uses the same native counterexample owner");
        foreach(var options in new[]{new CosmicEntityCompositionOptions(maximumInputPayloadBytes:1),new CosmicEntityCompositionOptions(maximumStageRows:1)})
        {
            var bounded=entity.Take(0).Result.Select(row=>new NativeProjectionProbe{Name=row.Name}).Result.Take(0).Result;
            var limited=await Read(bounded,row=>row.Name=="anything",options);
            Assert.AreEqual("CQL_QUERY_LIMIT",limited.ErrorCode,limited.Message+limited.Exception);Passed("entity source/work budget precedes empty native All");
        }
    }
}
