using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static Task VerifyEntityCardinality(ICosmicQueryStorageProvider adapter, Guid rootId,
        IQueryable<Holon> root, IQueryable<Holon> multi, List<object> checks)
    {
        const string rootName = "protected source root";
        var ordered = multi.OrderBy(row => row.Name);

        // The first page contains "owned cycle". Moving this predicate before
        // Take(1) would incorrectly select the matching root from the next page.
        Assert.AreEqual(0, ordered.Take(1).Count(row => row.Name == rootName));
        Passed("entity Count predicate stays after Take");
        Assert.AreEqual(0L, ordered.Take(1).LongCount(row => row.Name == rootName));
        Passed("entity LongCount predicate stays after Take");
        Assert.IsFalse(ordered.Take(1).Any(row => row.Name == rootName));
        Passed("entity Any predicate stays after Take");
        Assert.AreEqual(1, ordered.Skip(1).Take(1).Count(row => row.Name == rootName));
        Passed("entity Count retains offset and limit");
        Assert.AreEqual(1L, ordered.Skip(1).Take(1).LongCount(row => row.Name == rootName));
        Passed("entity LongCount retains offset and limit");
        Assert.IsTrue(ordered.Skip(1).Take(1).Any(row => row.Name == rootName));
        Passed("entity Any retains offset and limit");

        var tight = CosmicQueryTraversalOptions.Create(2, 1).Result;
        var sufficient = CosmicQueryTraversalOptions.Create(2, 4).Result;
        Assert.AreEqual(0, root.Take(0).Count(row => CosmicQueryRelations.AnyDescendant(row, child => true, tight)));
        Passed("empty entity page visits no terminal descendants");
        Assert.IsFalse(root.Take(0).Any(row => CosmicQueryRelations.AnyDescendant(row, child => true, tight)));
        Passed("empty entity Any visits no terminal descendants");
        Assert.AreEqual(1, root.Take(1).Count(row => row.Children.Any(child => child.Name == "visible match")));
        Passed("entity post-window child predicate");
        Assert.AreEqual(1L, root.Take(1).LongCount(row => CosmicQueryRelations.AnyDescendant(row,
            child => child.Name == "grandchild one", sufficient)));
        Passed("entity post-window descendant predicate");

        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() =>
            root.Take(1).Count(row => CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("entity Count preserves terminal traversal limit");
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() =>
            root.Take(1).LongCount(row => CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("entity LongCount preserves terminal traversal limit");
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() =>
            root.Take(1).Any(row => CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("entity Any preserves terminal traversal limit");
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() =>
            root.Take(1).Count(row => row.Name == rootName || CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("entity terminal true OR cannot hide protected errors");
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() =>
            root.Take(1).Count(row => row.Name == "absent" && CosmicQueryRelations.AnyDescendant(row, child => true, tight))).Code);
        Passed("entity terminal false AND cannot hide protected errors");

        var protectedSource = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.Id == rootId).WhereAnyDescendant(child => true, tight).Take(0).Result;
        var protectedRows = CosmicLinq.Create(NativeManager(adapter), adapter, protectedSource).Result;
        Assert.AreEqual("CQL_QUERY_LIMIT", Assert.ThrowsException<CosmicLinqException>(() =>
            protectedRows.Count(row => true)).Code);
        Passed("source traversal errors precede empty entity page");
        Assert.AreEqual(0, root.Where(row => row.Name == "absent").Take(2).Count(row => row.Name == rootName));
        Passed("post-window entity Count genuinely empty source");

        var requestedNames = new[] { rootName, "unmatched" };
        Assert.AreEqual(1, root.Take(1).Count(row => requestedNames.Contains(row.Name)));
        Passed("post-window entity membership predicate");
        Assert.AreEqual(1, root.Take(1).Count(row => CosmicQueryMetadata.Value<long>(row, "i64") == 9007199254740993L));
        Passed("post-window entity exact Int64 metadata predicate");
        var wanted = rootName;
        Assert.AreEqual(1, root.Take(1).Count(row => row.Name == wanted));
        wanted = "absent";
        Assert.AreEqual(0, root.Take(1).Count(row => row.Name == wanted));
        Passed("post-window entity terminal binds each execution");

        return Task.CompletedTask;
        void Passed(string label) => checks.Add(new { provider = adapter.QueryProviderType.ToString(), label });
    }
}
