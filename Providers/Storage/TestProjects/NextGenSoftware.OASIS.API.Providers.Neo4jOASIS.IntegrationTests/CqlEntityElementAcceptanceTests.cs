using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static void VerifyEntityElements(ICosmicQueryStorageProvider adapter,Guid rootId,List<object> checks)
    {
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result).Where(row=>row.Id==rootId).Result;
        var root=CosmicLinq.Create(NativeManager(adapter),adapter,source).Result.Take(1).Where(row=>row.Name=="protected source root");
        void Passed(string name)=>checks.Add(new{provider=adapter.QueryProviderType.ToString(),entityElement=name});
        Assert.AreEqual(rootId,root.First().Id);Passed("standard staged First");
        Assert.AreEqual(rootId,root.First(row=>row.Name=="protected source root").Id);Passed("standard staged First predicate");
        Assert.IsNull(root.FirstOrDefault(row=>row.Name=="missing"));Passed("standard staged First default false predicate");
        Assert.AreEqual(rootId,root.Single().Id);Passed("standard staged Single");
        Assert.AreEqual(rootId,root.Single(row=>row.Name=="protected source root").Id);Passed("standard staged Single predicate");
        Assert.IsNull(root.SingleOrDefault(row=>row.Name=="missing"));Passed("standard staged Single default false predicate");
        Assert.IsNull(root.Take(0).FirstOrDefault());Passed("standard staged First default empty page");
        Assert.IsNull(root.Take(0).SingleOrDefault());Passed("standard staged Single default empty page");
        Assert.AreEqual("CQL_SEQUENCE_EMPTY",Assert.ThrowsException<CosmicLinqException>(()=>root.Take(0).First()).Code);Passed("standard staged First empty error");
        var candidates=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row=>row.Id==rootId || row.ParentHolonId==rootId).OrderBy(row=>row.Name).Take(3).Result;
        var ordered=CosmicLinq.Create(NativeManager(adapter),adapter,candidates).Result.OrderBy(row=>row.Name);
        Assert.AreEqual("CQL_SEQUENCE_MULTIPLE",Assert.ThrowsException<CosmicLinqException>(()=>ordered.Single()).Code);Passed("standard staged Single ambiguity");
        Assert.AreEqual("CQL_SEQUENCE_MULTIPLE",Assert.ThrowsException<CosmicLinqException>(()=>ordered.SingleOrDefault()).Code);Passed("standard staged Single default ambiguity");
        Assert.AreEqual(rootId,ordered.ElementAt(1).Id);Passed("standard staged element index after source page");
        Assert.IsNull(ordered.ElementAtOrDefault(9));Passed("standard staged index default out of range");
        Assert.IsNull(ordered.ElementAtOrDefault(-1));Passed("standard staged negative index default");
        Assert.ThrowsException<ArgumentOutOfRangeException>(()=>ordered.ElementAt(-1));Passed("standard framework negative index error");
        var tight=CosmicQueryTraversalOptions.Create(2,1).Result;
        Assert.AreEqual("CQL_QUERY_LIMIT",Assert.ThrowsException<CosmicLinqException>(()=>root.First(row=>
            row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight))).Code);Passed("standard staged First protected true OR");
        Assert.AreEqual("CQL_QUERY_LIMIT",Assert.ThrowsException<CosmicLinqException>(()=>root.FirstOrDefault(row=>
            row.Name=="missing" && CosmicQueryRelations.AnyDescendant(row,child=>true,tight))).Code);Passed("standard staged First default protected false AND");
        Assert.IsNull(root.Take(0).FirstOrDefault(row=>CosmicQueryRelations.AnyDescendant(row,child=>true,tight)));Passed("standard staged earlier empty page has no terminal traversal");
        var unconstructible=CosmicQuery<NativeCountWithoutConstructor>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row=>row.Id==rootId || row.ParentHolonId==rootId).OrderBy(row=>row.Name).Take(3).Result;
        var native=CosmicLinq.Create(NativeManager(adapter),adapter,unconstructible).Result.OrderBy(row=>row.Name);
        Assert.AreEqual("CQL_SEQUENCE_MULTIPLE",Assert.ThrowsException<CosmicLinqException>(()=>native.Single()).Code);Passed("standard staged Single before DTO constructor");
        var selected=root.First();Assert.IsFalse(HolonChangeTracking.HasChanges(selected));
        selected.Name="local staged change";Assert.IsTrue(HolonChangeTracking.HasChanges(selected));Passed("standard staged element shared tracking");

        var fallback=new Holon {Name="caller-local-default"};var changed=HolonChangeTracking.HasChanges(fallback);
        var tail=root.OrderBy(row=>row.Name);
        Assert.AreSame(fallback,root.Take(0).FirstOrDefault(fallback));Passed("explicit First default after empty page");
        Assert.AreSame(fallback,root.Take(0).SingleOrDefault(fallback));Passed("explicit Single default after empty page");
        Assert.AreSame(fallback,tail.Take(0).LastOrDefault(fallback));Passed("explicit Last default after empty page");
        Assert.AreSame(fallback,root.FirstOrDefault(row=>row.Name=="missing",fallback));Passed("explicit First predicate default");
        Assert.AreSame(fallback,root.SingleOrDefault(row=>row.Name=="missing",fallback));Passed("explicit Single predicate default");
        Assert.AreSame(fallback,tail.LastOrDefault(row=>row.Name=="missing",fallback));Passed("explicit Last predicate default");
        Assert.AreEqual(rootId,root.FirstOrDefault(fallback).Id);Passed("present First ignores explicit default");
        Assert.AreEqual(rootId,root.SingleOrDefault(fallback).Id);Passed("present Single ignores explicit default");
        Assert.AreEqual(rootId,tail.LastOrDefault(fallback).Id);Passed("present Last ignores explicit default");
        Assert.AreEqual("CQL_SEQUENCE_MULTIPLE",Assert.ThrowsException<CosmicLinqException>(()=>ordered.SingleOrDefault(fallback)).Code);Passed("explicit default cannot conceal ambiguity");
        Assert.AreEqual("CQL_QUERY_LIMIT",Assert.ThrowsException<CosmicLinqException>(()=>root.FirstOrDefault(row=>
            row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight),fallback)).Code);Passed("explicit First default cannot conceal protected failure");
        Assert.AreEqual("CQL_QUERY_LIMIT",Assert.ThrowsException<CosmicLinqException>(()=>tail.LastOrDefault(row=>
            row.Name=="missing" && CosmicQueryRelations.AnyDescendant(row,child=>true,tight),fallback)).Code);Passed("explicit Last default cannot conceal protected failure");
        Assert.AreEqual("caller-local-default",fallback.Name);Assert.AreEqual(changed,HolonChangeTracking.HasChanges(fallback));
    }
}
