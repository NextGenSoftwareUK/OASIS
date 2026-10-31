using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static Task VerifySelectedCardinality(ICosmicQueryStorageProvider adapter,
        IQueryable<Holon> root, IQueryable<Holon> multi, List<object> checks)
    {
        const string rootName = "protected source root";
        var names = multi.Select(row => row.Name);

        Assert.AreEqual(3, names.Count());
        Passed("native selected Count without result stages");
        Assert.AreEqual(3L, names.LongCount());
        Passed("native selected LongCount without result stages");
        Assert.IsTrue(names.Any());
        Passed("native selected Any without result stages");

        // Scalar terminals must never invoke application DTO constructors.
        SelectedElementDto.Constructions = 0;
        var records = multi.Select(row => new SelectedElementDto { Label = row.Name });
        Assert.AreEqual(1, records.OrderBy(value => value.Label).Skip(1).Take(1).Count());
        Assert.AreEqual(0, SelectedElementDto.Constructions);
        Passed("selected record Count constructs no DTO");

        var ordered = names.OrderBy(value => value);
        Assert.AreEqual(1, ordered.Take(2).Count(value => value == rootName));
        Passed("selected Count predicate after page");
        Assert.AreEqual(0L, ordered.Take(1).LongCount(value => value == rootName));
        Passed("selected LongCount predicate cannot move before page");
        Assert.IsFalse(ordered.Take(1).Any(value => value == rootName));
        Passed("selected Any predicate cannot move before page");
        Assert.IsTrue(ordered.Take(2).Any(value => value == rootName));
        Passed("selected Any predicate includes matching paged value");

        Assert.AreEqual(0, names.Take(0).Count());
        Passed("selected Count empty window");
        Assert.AreEqual(0L, names.Take(0).LongCount());
        Passed("selected LongCount empty window");
        Assert.IsFalse(names.Take(0).Any());
        Passed("selected Any empty window");

        var absent = root.Where(row => row.Name == "no source for cardinality").Select(row => row.Name);
        Assert.AreEqual(0, absent.Count());
        Passed("native selected Count genuinely empty source");
        Assert.IsFalse(absent.Any());
        Passed("native selected Any genuinely empty source");

        var nullable = root.Select(row => CosmicQueryMetadata.Value<string>(row, "nullable"));
        Assert.AreEqual(1, nullable.Count());
        Passed("selected null still counts as one row");
        Assert.IsTrue(nullable.Any());
        Passed("selected null still participates in Any");
        Assert.AreEqual(0, ordered.Skip(9).Count());
        Passed("selected Count exhausted offset");

        Assert.AreEqual(2, multi.OrderBy(row => row.Name).Take(2).Select(row => row.Name).Count());
        Passed("selected Count retains source window");
        var wanted = rootName;
        var rebound = names.Where(value => value == wanted);
        Assert.AreEqual(1, rebound.Count());
        wanted = "absent";
        Assert.AreEqual(0, rebound.Count());
        Passed("selected Count binds predicate per execution");

        Assert.IsFalse(records.Any(value => value.Label == "absent"));
        Assert.AreEqual(0, SelectedElementDto.Constructions);
        Passed("selected record Any constructs no DTO");
        Assert.AreEqual(1, records.OrderBy(value => value.Label).Skip(1).Take(1).Count(value => value.Label == rootName));
        Assert.AreEqual(0, SelectedElementDto.Constructions);
        Passed("selected record Count predicate remains after window");

        return Task.CompletedTask;
        void Passed(string label) => checks.Add(new { provider = adapter.QueryProviderType.ToString(), label });
    }
}
