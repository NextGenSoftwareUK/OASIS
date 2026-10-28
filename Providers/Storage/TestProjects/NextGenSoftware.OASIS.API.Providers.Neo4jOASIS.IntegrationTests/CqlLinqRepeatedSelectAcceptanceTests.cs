using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    public sealed class ReboundDto
    {
        public static int Constructions;
        public ReboundDto() => Constructions++;
        public decimal Amount { get; set; }
        public string Text { get; set; } = "";
        public decimal Copy { get; set; }
    }

    private static async Task VerifyRepeatedSelect(ICosmicQueryStorageProvider adapter, IQueryable<Holon> root,
        IQueryable<Holon> multi, List<object> checks)
    {
        var before = checks.Count;
        StagedWideDto.Constructions = 0;
        ReboundDto.Constructions = 0;
        var initial = multi.Select(row => new StagedWideDto
        {
            Label = row.Name,
            Amount = CosmicQueryMetadata.Value<decimal>(row, "wideDecimal"),
            Counter = CosmicQueryMetadata.Value<ulong>(row, "wideU64")
        }).OrderBy(row => row.Label).Take(3);
        var rebound = initial.Select(row => new ReboundDto { Amount = row.Amount, Text = row.Label, Copy = row.Amount });
        var rows = await rebound.ToCosmicListAsync();
        Assert.IsFalse(rows.IsError, rows.Message + rows.Exception);
        CollectionAssert.AreEqual(new[] { "owned cycle", "protected source root", "visible match" }, rows.Result.Select(row => row.Text).ToArray());
        Assert.AreEqual(3, ReboundDto.Constructions);
        Assert.AreEqual(0, StagedWideDto.Constructions);
        Passed("repeated record rename reorder duplicate async rows without intermediate DTO");
        CollectionAssert.AreEqual(new[] { -decimal.MaxValue, decimal.MaxValue, 0.0000000000000000000000000001m }, rows.Result.Select(row => row.Amount).ToArray());
        Assert.IsTrue(rows.Result.All(row => row.Copy == row.Amount));
        Passed("repeated record exact duplicate physical columns");

        var amounts = rebound.Select(row => row.Amount);
        Assert.AreEqual(0.0000000000000000000000000001m, amounts.Sum());
        Passed("reselected scalar Sum binds nonzero physical column");
        Assert.AreEqual(-decimal.MaxValue, amounts.Min());
        Passed("reselected scalar Min preserves bindings");
        Assert.AreEqual(decimal.MaxValue, amounts.Max());
        Passed("reselected scalar Max preserves bindings");
        Assert.AreEqual(3, amounts.Count());
        Passed("reselected scalar Count");
        Assert.AreEqual(3L, amounts.LongCount());
        Passed("reselected scalar LongCount");
        Assert.IsTrue(amounts.Any());
        Passed("reselected scalar Any");
        Assert.IsTrue(amounts.All(value => value >= -decimal.MaxValue));
        Passed("reselected scalar All physical predicate");
        Assert.AreEqual(-decimal.MaxValue, amounts.First());
        Passed("reselected scalar First");
        Assert.AreEqual(0.0000000000000000000000000001m, amounts.Last());
        Passed("reselected scalar Last");
        Assert.AreEqual(decimal.MaxValue, amounts.ElementAt(1));
        Passed("reselected scalar ElementAt");
        Assert.AreEqual("CQL_SEQUENCE_MULTIPLE", Assert.ThrowsException<CosmicLinqException>(() => amounts.Single()).Code);
        Passed("reselected scalar Single cardinality");
        Assert.AreEqual(decimal.MaxValue, rebound.Skip(1).Take(1).Select(row => row.Copy).Single());
        Passed("reselection after window preserves duplicate column");
        Assert.AreEqual(ulong.MaxValue, initial.Skip(1).Take(1).Select(row => row.Counter).Max());
        Passed("reselected UInt64 extremum follows earlier page");
        Assert.AreEqual(2, initial.Where(row => row.Counter > 0UL).Select(row => row.Label).Count());
        Passed("dropped column still owns earlier predicate");
        Assert.AreEqual(0, initial.Take(1).Where(row => row.Counter > 0UL).Select(row => row.Label).Count());
        Passed("dropped predicate cannot move before earlier window");
        var threshold = 0m;
        var captured = initial.Where(row => row.Amount > threshold).Select(row => new { Renamed = row.Label }).Select(row => row.Renamed);
        Assert.AreEqual(2, captured.Count());
        threshold = 1m;
        Assert.AreEqual(1, captured.Count());
        Passed("repeated Select binds earlier closure per execution");
        Assert.AreEqual("protected source root", captured.Single());
        Passed("repeated Select retained closure matching row");
        Assert.AreEqual("owned cycle", rebound.Select(row => row).Select(row => new { Title = row.Text }).Select(row => row.Title).First());
        Passed("record identity then triple renamed Select");
        Assert.AreEqual("owned cycle", initial.Select(row => row.Label).Select(value => value).First());
        Passed("scalar identity repeated Select");
        Assert.AreEqual("protected source root", root.Select(row => new { row.Name, row.Version }).Select(row => row.Name).Single());
        Passed("unstaged repeated Select ordinary native projection");
        Assert.AreEqual("protected source root", root.Select(row => new { row.Name, row.Version }).Select(row => row.Name).Min());
        Passed("unstaged reselected scalar extremum");
        Assert.AreEqual("CQL_PROJECTION_UNSUPPORTED", Assert.ThrowsException<CosmicLinqException>(() => rebound.Select(row => row.Amount + 1m)).Code);
        Passed("reselected computations explicitly unsupported");
        Assert.ThrowsException<CosmicLinqException>(() => rebound.Select((row, index) => row.Text));
        Passed("indexed reselection explicitly unsupported");
        Assert.AreEqual(0, StagedWideDto.Constructions);
        Assert.AreEqual(3, ReboundDto.Constructions);
        Passed("all reselected terminals avoid every intermediate record");
        Assert.AreEqual(26, checks.Count - before);

        void Passed(string label) => checks.Add(new { provider = adapter.QueryProviderType.ToString(), label });
    }
}
