using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlSingleTests
{
    [TestMethod]
    public void SingleSelectedPacketsRequireExactWidenedFiniteBinary32()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<float>(h, "reading")).Result;
        IReadOnlyList<object> Packet(object value) => new object[]
            { new Dictionary<string, object> { ["s"] = 2L, ["v"] = value } };

        foreach (var value in new[] { (double)float.MinValue, (double)-float.Epsilon, 0d, (double)float.Epsilon, (double)float.MaxValue })
        {
            var row = Neo4jOASIS.ReadNativeProjectionRow(Packet(value), query.Columns);
            Assert.AreEqual((float)value, row.Values[0]);
        }
        foreach (var value in new object[] { 0.1d, double.Epsilon, double.MaxValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity, "1.0", 1L, 1m })
            Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.ReadNativeProjectionRow(Packet(value), query.Columns));
    }

    [TestMethod]
    public void NegativeZeroSelectedPacketPublishesCanonicalZero()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<float>(h, "reading")).Result;
        var row = Neo4jOASIS.ReadNativeProjectionRow(new object[]
            { new Dictionary<string, object> { ["s"] = 2L, ["v"] = -0d } }, query.Columns);
        Assert.AreEqual(0, BitConverter.SingleToInt32Bits((float)row.Values[0]));
    }
}
