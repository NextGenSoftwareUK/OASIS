using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlDoubleTests
{
    [TestMethod]
    public void DoubleSelectedPacketsRequireFiniteNativeBinary64()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<double>(h, "reading")).Result;
        IReadOnlyList<object> Packet(object value) => new object[]
            { new Dictionary<string, object> { ["s"] = 2L, ["v"] = value } };

        foreach (var value in new[] { double.MinValue, -double.Epsilon, 0d, double.Epsilon, double.MaxValue })
        {
            var row = Neo4jOASIS.ReadNativeProjectionRow(Packet(value), query.Columns);
            Assert.AreEqual(value, row.Values[0]);
        }
        foreach (var value in new object[] { double.NaN, double.NegativeInfinity, double.PositiveInfinity, "1.0", 1L, 1m })
            Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.ReadNativeProjectionRow(Packet(value), query.Columns));
    }

    [TestMethod]
    public void NegativeZeroSelectedPacketPublishesCanonicalZero()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<double>(h, "reading")).Result;
        var row = Neo4jOASIS.ReadNativeProjectionRow(new object[]
            { new Dictionary<string, object> { ["s"] = 2L, ["v"] = -0d } }, query.Columns);
        Assert.AreEqual(0L, BitConverter.DoubleToInt64Bits((double)row.Values[0]));
    }
}
