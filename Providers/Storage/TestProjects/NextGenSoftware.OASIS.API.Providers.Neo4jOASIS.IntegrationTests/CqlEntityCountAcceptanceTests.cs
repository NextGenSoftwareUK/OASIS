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

        var root=CosmicLinq.Create(NativeManager(adapter),adapter,source).Result.Where(row=>row.Name=="protected source root");
        Assert.AreEqual(1,root.Count());Passed("standard staged Count");
        Assert.AreEqual(1L,root.LongCount());Passed("standard staged LongCount");
        Assert.IsTrue(root.Any());Passed("standard staged Any");
        Assert.AreEqual(0,root.Where(row=>row.Name=="missing").Count());Passed("standard staged false Count");
        Assert.IsFalse(root.Where(row=>row.Name=="missing").Any());Passed("standard staged false Any");
        Assert.IsTrue(root.All(row=>row.Name=="protected source root"));Passed("standard staged All true");
        Assert.IsFalse(root.All(row=>row.Name=="missing"));Passed("standard staged All false");
        Assert.IsFalse(root.Take(0).Any());Passed("standard staged empty Any");
        Assert.IsTrue(root.Take(0).All(row=>false));Passed("standard staged vacuous All");
        Assert.AreEqual(1,root.Take(1).Count(row=>row.Name=="protected source root"));Passed("standard staged Count predicate after page");
        Assert.AreEqual(1L,root.Take(1).LongCount(row=>row.Name=="protected source root"));Passed("standard staged LongCount predicate after page");
        Assert.IsTrue(root.Take(1).Any(row=>row.Name=="protected source root"));Passed("standard staged Any predicate after page");
        var name="missing";var deferred=root.Where(row=>row.Name==name);
        Assert.AreEqual(0,deferred.Count());name="protected source root";Assert.AreEqual(1,deferred.Count());Passed("standard staged count closure rebinding");
        Assert.AreEqual("CQL_QUERY_LIMIT",Assert.ThrowsException<CosmicLinqException>(()=>root.All(row=>
            row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight))).Code);
        Passed("standard staged All retains protected true OR");
        Assert.AreEqual("CQL_QUERY_LIMIT",Assert.ThrowsException<CosmicLinqException>(()=>root.Where(row=>
            row.Name=="missing" && CosmicQueryRelations.AnyDescendant(row,child=>true,tight)).Take(0).Count()).Code);
        Passed("standard staged Count retains protected false AND before empty");
        var noConstructor=CosmicLinq.Create(NativeManager(adapter),adapter,typed.Source).Result.Where(row=>row.Name=="protected source root");
        Assert.AreEqual(1,noConstructor.Count());Passed("standard staged Count without DTO construction or graph expansion");
    }
}
