using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    public sealed class NativeCountWithoutConstructor : Holon
    {
        public NativeCountWithoutConstructor() => throw new InvalidOperationException("Native cardinality cannot hydrate application entities.");
    }
    private static async Task VerifyEntityCount(ICosmicQueryEntityCountStorageProvider adapter,Guid rootId,List<object> checks)
    {
        async Task<NextGenSoftware.OASIS.Common.OASISResult<CosmicScalarExecution<long>>> Count<T>(
            CosmicEntityCompositionQuery<T> query,CosmicEntityCompositionOptions? options=null) where T:IHolon
        {
            var plan=CosmicEntityCompositionPlanner.Plan(query,adapter.QueryProviderType,
                new CosmicQueryTarget(adapter.QueryProviderType,adapter.QueryCapabilities),adapter.EntityCompositionCapabilities,options);
            Assert.IsFalse(plan.IsError,plan.Message);
            return await NativeManager(adapter).ExecuteEntityCountQueryAsync(plan.Result,new ICosmicQueryStorageProvider[] {adapter});
        }
        void Passed(string name)=>checks.Add(new {provider=adapter.QueryProviderType.ToString(),entityCount=name});
        async Task Value(CosmicEntityCompositionQuery<Holon> query,long expected,string name)
        {
            var read=await Count(query); Assert.IsFalse(read.IsError,read.Message+read.Exception); Assert.IsTrue(read.IsLoaded);
            Assert.AreEqual(expected,read.Result.Providers.Single().Value); Passed(name);
        }
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result).Where(row=>row.Id==rootId).Take(1).Result;
        await Value(source.Compose().Result.Where(row=>row.Name=="protected source root").Result,1,"matching native stage count");
        await Value(source.Compose().Result.Where(row=>row.Name=="missing").Result,0,"false native stage count");
        await Value(source.Compose().Result.OrderBy(row=>row.Name).Result.Skip(1).Result.Take(2).Result,0,"ordered native stage offset");
        var tight=CosmicQueryTraversalOptions.Create(2,1).Result;
        var protectedQueries=new[] {
            source.Compose().Result.Where(row=>row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight)).Result.Take(0).Result,
            source.Compose().Result.Where(row=>row.Name=="missing" && CosmicQueryRelations.AnyDescendant(row,child=>true,tight)).Result.Take(0).Result };
        foreach(var query in protectedQueries)
        {
            var rejected=await Count(query); Assert.AreEqual("CQL_QUERY_LIMIT",rejected.ErrorCode,rejected.Message+rejected.Exception);
            Passed("protected count before later empty stage");
        }
        var candidates=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row=>row.Id==rootId || row.ParentHolonId==rootId).Take(3).Result.Compose().Result.Where(row=>false).Result.Take(0).Result;
        foreach(var options in new[] {new CosmicEntityCompositionOptions(maximumInputRows:1),
            new CosmicEntityCompositionOptions(maximumInputPayloadBytes:1),new CosmicEntityCompositionOptions(maximumStageRows:1)})
        {
            var rejected=await Count(candidates,options); Assert.AreEqual("CQL_QUERY_LIMIT",rejected.ErrorCode,rejected.Message+rejected.Exception);
            Passed("native count input/work budget");
        }
        var typed=CosmicQuery<NativeCountWithoutConstructor>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row=>row.Id==rootId).WithChildren(CosmicQueryGraphOptions.Create(maximumNodes:1).Result).Take(1).Result
            .Compose().Result.Where(row=>row.Name=="protected source root").Result;
        var native=await Count(typed); Assert.IsFalse(native.IsError,native.Message+native.Exception);
        Assert.AreEqual(1L,native.Result.Providers.Single().Value); Passed("native count without DTO constructors or graph expansion");
    }
}
