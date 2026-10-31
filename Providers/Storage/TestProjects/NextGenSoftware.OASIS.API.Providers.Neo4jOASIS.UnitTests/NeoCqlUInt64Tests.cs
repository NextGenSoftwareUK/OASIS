using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlUInt64Tests
{
    [TestMethod]
    public void UnsignedGroupedLiteralsBindExactOrdinalKeysRatherThanAverageKeys()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => CosmicQueryMetadata.Value<ulong>(h, "counter"))
            .Select(g => g.Key).Where(value => value == ulong.MaxValue).OrderBy(value => value).Result;
        var command = Neo4jOASIS.CompileCqlGrouped(query, 10);
        Assert.IsTrue(command.Parameters.Values.Contains("18446744073709551615"));
        Assert.IsFalse(command.Parameters.Values.Any(value => value is ulong || value is decimal || value is double));
        Assert.IsFalse(command.Query.Contains("toFloat"));
        Assert.IsFalse(command.Query.Contains("toInteger"));
    }

    [TestMethod]
    public void UnsignedMaximumAndZeroUseTwentyDigitKeys()
    {
        Assert.AreEqual("18446744073709551615", NeoCqlUInt64.Literal(ulong.MaxValue));
        Assert.AreEqual("00000000000000000000", NeoCqlUInt64.Literal(0UL));
    }

    [DataTestMethod]
    [DataRow("-1")] [DataRow("0.5")] [DataRow("18446744073709551616")]
    public void UnsignedLiteralRejectsRoundingAndOverflow(string text)
        => Assert.ThrowsException<NotSupportedException>(() => NeoCqlUInt64.Literal(
            decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture)));
}
