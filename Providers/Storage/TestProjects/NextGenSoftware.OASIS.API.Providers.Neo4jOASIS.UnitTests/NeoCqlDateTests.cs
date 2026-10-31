using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlDateTests
{
    [TestMethod]
    public void UtcTicksBindAsCanonicalTextWithoutDriverTemporalConversion()
    {
        var utc = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc).AddTicks(1);
        var query = CosmicQuery<Holon>.Create().GroupBy(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp"))
            .Select(g => g.Key).Where(value => value == utc).OrderBy(value => value).Result;
        var command = Neo4jOASIS.CompileCqlGrouped(query, 10);
        Assert.IsTrue(command.Parameters.Values.Contains("2026-10-24T00:00:00.0000001Z"));
        Assert.IsFalse(command.Parameters.Values.Any(value => value is DateTime || value is double));
        Assert.IsFalse(command.Query.Contains("toFloat"));
        Assert.IsFalse(command.Query.Contains("epochMillis"));
    }

    [TestMethod]
    public void LocalAndUnspecifiedLiteralsCannotAcquireUtcMeaning()
    {
        foreach (var kind in new[] { DateTimeKind.Local, DateTimeKind.Unspecified })
        {
            var date = new DateTime(2026, 10, 24, 0, 0, 0, kind);
            Assert.ThrowsException<ArgumentException>(() => NeoCqlDate.Literal(date));
            var query = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp") == date);
            Assert.IsTrue(query.IsError);
        }
    }
}
