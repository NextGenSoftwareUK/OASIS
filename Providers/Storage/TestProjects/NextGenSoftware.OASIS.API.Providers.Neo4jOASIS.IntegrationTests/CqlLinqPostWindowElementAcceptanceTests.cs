using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static void VerifyPostWindowElements(ICosmicQueryStorageProvider adapter, Guid rootId, Guid actor,
        IQueryable<Holon> root, IQueryable<Holon> multi, List<object> checks)
    {
        if (adapter.QueryProviderType != ProviderType.SQLLiteDBOASIS) return;
        var before = checks.Count;
        const string name = "protected source root";
        var ordered = multi.OrderBy(row => row.Name);
        var page = ordered.Skip(1).Take(1);
        Assert.AreEqual(rootId, page.First(row => row.Name == name).Id);
        Passed("post-window entity First");
        Assert.AreEqual(rootId, page.FirstOrDefault(row => row.Name == name)!.Id);
        Passed("post-window entity FirstOrDefault");
        Assert.AreEqual(rootId, page.Single(row => row.Name == name).Id);
        Passed("post-window entity Single");
        Assert.AreEqual(rootId, page.SingleOrDefault(row => row.Name == name)!.Id);
        Passed("post-window entity SingleOrDefault");
        Assert.AreEqual(rootId, page.Last(row => row.Name == name).Id);
        Passed("post-window entity Last");
        Assert.AreEqual(rootId, page.LastOrDefault(row => row.Name == name)!.Id);
        Passed("post-window entity LastOrDefault");
        Assert.IsNull(ordered.Take(1).FirstOrDefault(row => row.Name == name));
        Passed("First predicate cannot move before page");
        Assert.IsNull(ordered.Take(1).SingleOrDefault(row => row.Name == name));
        Passed("Single predicate cannot move before page");
        Assert.IsNull(ordered.Take(1).LastOrDefault(row => row.Name == name));
        Passed("Last predicate cannot move before page");
        Assert.AreEqual("visible match", ordered.Take(3).Last(row => row.Name != "missing").Name);
        Passed("Last filters complete source page before reversal");
        Assert.AreEqual("CQL_SEQUENCE_MULTIPLE", Assert.ThrowsException<CosmicLinqException>(() => ordered.Take(3).Single(row => true)).Code);
        Passed("post-window Single preserves multiple error");
        var supplied = new Holon { Name = "caller default" };
        Assert.AreSame(supplied, page.FirstOrDefault(row => row.Name == "missing", supplied));
        Passed("First supplied empty default");
        Assert.AreSame(supplied, page.SingleOrDefault(row => row.Name == "missing", supplied));
        Passed("Single supplied empty default");
        Assert.AreSame(supplied, page.LastOrDefault(row => row.Name == "missing", supplied));
        Passed("Last supplied empty default");
        Assert.AreEqual("CQL_SEQUENCE_MULTIPLE", Assert.ThrowsException<CosmicLinqException>(() => ordered.Take(3).SingleOrDefault(row => true, supplied)).Code);
        Passed("supplied default cannot mask multiple");
        Assert.AreEqual("CQL_SEQUENCE_EMPTY", Assert.ThrowsException<CosmicLinqException>(() => page.First(row => false)).Code);
        Passed("First required empty error");
        Assert.AreEqual("CQL_SEQUENCE_EMPTY", Assert.ThrowsException<CosmicLinqException>(() => page.Single(row => false)).Code);
        Passed("Single required empty error");
        Assert.AreEqual("CQL_SEQUENCE_EMPTY", Assert.ThrowsException<CosmicLinqException>(() => page.Last(row => false)).Code);
        Passed("Last required empty error");
        var wanted = name;
        Assert.AreEqual(rootId, page.First(row => row.Name == wanted).Id);
        wanted = "missing";
        Assert.IsNull(page.FirstOrDefault(row => row.Name == wanted));
        Passed("terminal predicate capture per execution");
        Assert.AreEqual(rootId, page.First(row => CosmicQueryMetadata.Value<long>(row, "i64") == 9007199254740993L).Id);
        Passed("post-window element exact Int64 predicate");
        var names = new[] { name, "missing" };
        Assert.AreEqual(rootId, page.Single(row => names.Contains(row.Name)).Id);
        Passed("post-window element membership predicate");
        Assert.IsNull(root.Where(row => row.Name == "missing").Take(2).FirstOrDefault(row => true));
        Passed("genuinely empty source terminal");

        var tight = CosmicQueryTraversalOptions.Create(2, 1).Result;
        var sufficient = CosmicQueryTraversalOptions.Create(2, 4).Result;
        Assert.IsNull(root.Take(0).FirstOrDefault(row => CosmicQueryRelations.AnyDescendant(row, child => true, tight)));
        Passed("empty page visits no terminal descendants");
        Assert.AreEqual(rootId, root.Take(1).First(row => row.Children.Any(child => child.Name == "visible match")).Id);
        Passed("post-window element child predicate");
        Assert.AreEqual(rootId, root.Take(1).Single(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "grandchild one", sufficient)).Id);
        Passed("post-window element descendant predicate");
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() => root.Take(1).FirstOrDefault(row => CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("First default cannot hide traversal limit");
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() => root.OrderBy(row => row.Name).Take(1).LastOrDefault(row => CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("Last default cannot hide traversal limit");
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() => root.Take(1).Single(row => CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("Single retains traversal limit");
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() => root.Take(1).First(row => row.Name == name || CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("true OR cannot hide terminal scope error");
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() => root.Take(1).FirstOrDefault(row => row.Name == "missing" && CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("false AND cannot hide terminal scope error");
        var protectedSource = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.Id == rootId).WhereAnyDescendant(child => true, tight).Take(0).Result;
        var protectedRows = CosmicLinq.Create(NativeManager(adapter), adapter, protectedSource).Result;
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() => protectedRows.FirstOrDefault(row => true)).Code);
        Passed("source scope errors precede empty terminal page");
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() =>
            ordered.Take(3).First(row => row.Name != "missing" || CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("First limit cannot hide later paged terminal error");
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() =>
            ordered.Take(3).Last(row => row.Name != "missing" || CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("Last limit cannot hide earlier paged terminal error");
        IQueryable<Holon> Graph(CosmicQueryGraphOptions options)
        {
            var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
                .Where(row => row.Id == rootId).Result.WithChildren(options).Result;
            return CosmicLinq.Create(NativeManager(adapter), adapter, source).Result;
        }
        var shallow = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot,
            maxChildDepth: 1, maximumNodes: 10).Result;
        var graph = Graph(shallow).Take(1).Single(row => row.Name == name);
        Assert.AreEqual(2, graph.Children.Count());
        Assert.IsTrue(graph.Children.All(child => !child.Children.Any()));
        Passed("post-window Single expands snapshot graph to depth1");
        var deep = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot,
            maxChildDepth: 2, maximumNodes: 10).Result;
        var expanded = Graph(deep).OrderBy(row => row.Name).Take(1).Last(row => row.Name == name);
        Assert.AreEqual(2, expanded.Children.Count());
        Assert.AreEqual(2, expanded.Children.Single(child => child.Name == "private branch").Children.Count());
        Passed("post-window Last expands snapshot graph to depth2");
        var bounded = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot,
            maxChildDepth: 2, maximumNodes: 2).Result;
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() => Graph(bounded).Take(1).First(row => true)).Code);
        Passed("post-window element graph retains node budget");
        Assert.IsNull(Graph(bounded).Take(1).FirstOrDefault(row => false));
        Passed("terminal rejection performs no graph expansion");
        var visibleSource = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.Id == rootId).Result.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        IQueryable<Holon> Bound(CosmicQuery<Holon> source) => CosmicLinq.Create(NativeManager(adapter), adapter, source).Result;
        var visible = Bound(visibleSource).Take(1);
        Assert.AreEqual(rootId, visible.First(row => row.Children.Any(child => child.Name == "visible match")).Id);
        Passed("terminal child predicate preserves owner public visibility");
        Assert.IsNull(visible.FirstOrDefault(row => row.Children.Any(child => child.Name == "private branch")));
        Passed("terminal child predicate cannot expose private child");
        Assert.IsNull(visible.FirstOrDefault(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "grandchild one", sufficient)));
        Passed("terminal descendants cannot cross hidden intermediate parent");
        var visibleGraph = Bound(visibleSource.WithChildren(deep).Result).Take(1).Single(row => true);
        Assert.AreEqual(1, visibleGraph.Children.Count());
        Assert.AreEqual("visible match", visibleGraph.Children.Single().Name);
        Passed("post-window expanded graph preserves owner public visibility");
        var visibleBounded = Bound(visibleSource.WithChildren(bounded).Result).Take(1).First(row => true);
        Assert.AreEqual(1, visibleBounded.Children.Count());
        Passed("hidden graph nodes do not consume returned node budget");
        var catalogue = CosmicQueryReadAccess.AuthoritativeRecords(actor, adapter.QueryProviderType, new[] { rootId }).Result;
        var catalogSource = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.Id == rootId).Result.WithReadAccess(catalogue).Result;
        Assert.IsNull(Bound(catalogSource).Take(1).FirstOrDefault(row => row.Children.Any(child => true)));
        Passed("authoritative root catalogue does not authorize public children");
        Assert.IsFalse(Bound(catalogSource.WithChildren(deep).Result).Take(1).First(row => true).Children.Any());
        Passed("expanded terminal graph respects authoritative catalogue");
        var denied = CosmicQueryReadAccess.AuthoritativeRecords(actor, adapter.QueryProviderType, Array.Empty<Guid>()).Result;
        var deniedSource = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.Id == rootId).Result.WithReadAccess(denied).Result;
        Assert.IsNull(Bound(deniedSource).Take(1).FirstOrDefault(row => true));
        Passed("terminal cannot return root outside authoritative catalogue");
        Assert.AreEqual(before + 45, checks.Count);
        void Passed(string label) => checks.Add(new { provider = adapter.QueryProviderType.ToString(), label });
    }
}
