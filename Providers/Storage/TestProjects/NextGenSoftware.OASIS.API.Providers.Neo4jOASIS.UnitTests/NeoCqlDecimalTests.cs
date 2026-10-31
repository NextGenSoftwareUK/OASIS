using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlDecimalTests
{
    [TestMethod]
    public void DecimalGroupedLiteralsUseTheSameCoreKeysAsNativeValues()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => CosmicQueryMetadata.Value<decimal>(h, "price"))
            .Select(g => g.Key).Where(value => value == decimal.MinValue).OrderBy(value => value).Result;
        var command = Neo4jOASIS.CompileCqlGrouped(query, 10);
        Assert.IsTrue(command.Parameters.Values.Contains(CosmicNumericScalarResults.DecimalOrderKey(decimal.MinValue)));
        Assert.IsFalse(command.Parameters.Values.Any(value => value is decimal || value is double));
        Assert.IsFalse(command.Query.Contains("toFloat"));
        Assert.AreEqual(NeoCqlDecimal.Literal(1m), NeoCqlDecimal.Literal(1.000m));
    }

    [TestMethod]
    public void DecimalSelectedPacketsRequireCanonicalExactKeys()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<decimal>(h, "price")).Result;
        IReadOnlyList<object> Packet(string key) => new object[] { new Dictionary<string, object> { ["s"] = 2L, ["v"] = key } };
        foreach (var value in new[] { decimal.MinValue, decimal.MaxValue, -1e-28m, 0m, 1e-28m })
        {
            var row = Neo4jOASIS.ReadNativeProjectionRow(Packet(CosmicNumericScalarResults.DecimalOrderKey(value)), query.Columns);
            Assert.AreEqual(value, row.Values[0]);
        }
        foreach (var key in new[] { "1.0", "NaN", new string('0', 58), "1" + new string('9', 57) })
            Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.ReadNativeProjectionRow(Packet(key), query.Columns));
    }
}
