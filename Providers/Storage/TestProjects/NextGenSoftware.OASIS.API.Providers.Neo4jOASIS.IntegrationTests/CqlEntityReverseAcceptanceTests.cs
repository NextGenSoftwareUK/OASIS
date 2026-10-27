using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyEntityReverse(ICosmicQueryEntityCompositionStorageProvider adapter,Guid rootId,List<object> checks)
    {
        CosmicEntityCompositionPlan<Holon> Plan(CosmicEntityCompositionQuery<Holon> query)
        {
            var plan=CosmicEntityCompositionPlanner.Plan(query,adapter.QueryProviderType,
                new CosmicQueryTarget(adapter.QueryProviderType,adapter.QueryCapabilities),adapter.EntityCompositionCapabilities);
            Assert.IsFalse(plan.IsError,plan.Message);return plan.Result;
        }
        async Task<Holon[]> Read(CosmicEntityCompositionQuery<Holon> query)
        {
            var read=await NativeManager(adapter).ExecuteTypedEntityCompositionQueryAsync(Plan(query),new ICosmicQueryStorageProvider[] {adapter},10);
            Assert.IsFalse(read.IsError,read.Message+read.Exception);return read.Result.Providers.Single().Rows.ToArray();
        }
        void Passed(string name)=>checks.Add(new{provider=adapter.QueryProviderType.ToString(),entityReverse=name});
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row=>row.Id==rootId || row.ParentHolonId==rootId).OrderBy(row=>row.Name).Take(3).Result;
        var original=source.Compose().Result.Skip(0).Result;
        var baseline=await Read(original);
        var reversed=await Read(original.Reverse().Result);
        CollectionAssert.AreEqual(baseline.Select(row=>row.Id).Reverse().ToArray(),reversed.Select(row=>row.Id).ToArray());Passed("reverse original source page");
        var page=await Read(original.Reverse().Result.Take(2).Result.Reverse().Result);
        CollectionAssert.AreEqual(baseline.Select(row=>row.Id).Reverse().Take(2).Reverse().ToArray(),page.Select(row=>row.Id).ToArray());Passed("reverse retains intermediate window");
        var restored=await Read(original.Reverse().Result.Reverse().Result);
        CollectionAssert.AreEqual(baseline.Select(row=>row.Id).ToArray(),restored.Select(row=>row.Id).ToArray());Passed("double reverse restores sequence");
        var ordered=await Read(original.OrderBy(row=>row.Version).Result.Reverse().Result);
        var expected=baseline.OrderBy(row=>row.Version).ThenBy(row=>row.Id.ToString("D"),StringComparer.Ordinal).Reverse().Select(row=>row.Id).ToArray();
        CollectionAssert.AreEqual(expected,ordered.Select(row=>row.Id).ToArray());Passed("reverse includes identity ties");
        var tight=CosmicQueryTraversalOptions.Create(2,1).Result;
        var protectedQuery=source.Compose().Result.Where(row=>row.Name!="missing" ||
            CosmicQueryRelations.AnyDescendant(row,child=>true,tight)).Result.Reverse().Result.Take(0).Result;
        var failed=await NativeManager(adapter).ExecuteTypedEntityCompositionQueryAsync(Plan(protectedQuery),new ICosmicQueryStorageProvider[] {adapter},10);
        Assert.AreEqual("CQL_QUERY_LIMIT",failed.ErrorCode,failed.Message+failed.Exception);Passed("reverse keeps preceding protected failure");
        var count=await NativeManager(adapter).ExecuteEntityCountQueryAsync(Plan(original.Reverse().Result.Take(2).Result),new ICosmicQueryStorageProvider[] {adapter});
        Assert.IsFalse(count.IsError,count.Message+count.Exception);Assert.AreEqual(2L,count.Result.Providers.Single().Value);Passed("count admits native reversal");

        var query=CosmicLinq.Create(NativeManager(adapter),adapter,source).Result.Reverse();
        CollectionAssert.AreEqual(baseline.Select(row=>row.Id).Reverse().ToArray(),query.ToArray().Select(row=>row.Id).ToArray());Passed("standard Reverse");
        CollectionAssert.AreEqual(baseline.Select(row=>row.Id).Reverse().Take(2).Reverse().ToArray(),query.Take(2).Reverse().ToArray().Select(row=>row.Id).ToArray());Passed("standard Reverse preserves intermediate page");
        Assert.AreEqual(baseline[0].Id,query.Last().Id);Passed("standard staged Last");
        Assert.AreEqual(rootId,query.Last(row=>row.Name=="protected source root").Id);Passed("standard staged Last predicate");
        Assert.IsNull(query.LastOrDefault(row=>row.Name=="missing"));Passed("standard staged Last default false predicate");
        Assert.IsNull(query.Take(0).LastOrDefault());Passed("standard staged Last empty default");
        Assert.AreEqual(rootId,query.ElementAt(^2).Id);Passed("standard staged from-end index");
        Assert.IsNull(query.ElementAtOrDefault(^9));Passed("standard staged from-end missing default");
        Assert.AreEqual("CQL_QUERY_LIMIT",Assert.ThrowsException<CosmicLinqException>(()=>query.Last(row=>row.Name!="missing" ||
            CosmicQueryRelations.AnyDescendant(row,child=>true,tight))).Code);Passed("standard staged Last protected scope");
        Assert.AreEqual(2,query.Take(2).Reverse().Count());Passed("standard count after native reversal");
        Assert.IsNull(query.ElementAtOrDefault(^0));Assert.ThrowsException<ArgumentOutOfRangeException>(()=>query.ElementAt(^0));Passed("standard zero tail index semantics");
        Assert.AreEqual("CQL_SEQUENCE_EMPTY",Assert.ThrowsException<CosmicLinqException>(()=>query.Take(0).Last()).Code);Passed("standard staged Last empty error");
    }
}
