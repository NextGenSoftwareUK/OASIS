using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.UnitTests;

[TestClass]
public class InfluxDbFluxBoundaryTests
{
    [DataTestMethod]
    [DataRow("${die(msg: \"injected\")}")]
    [DataRow("bucket\"\\\n\r\t")]
    [DataRow("日本語☕")]
    public void FluxLiteralPreservesUtf8WithoutCreatingCodeOrInterpolation(string value)
    {
        var literal = InfluxDBCqlCompiler.FluxStringLiteral(value);
        Assert.AreEqual('"', literal[0]); Assert.AreEqual('"', literal[^1]);
        Assert.IsFalse(literal.Contains("${"));
        var encoded = literal[1..^1];
        Assert.AreEqual(0, encoded.Length % 4);
        var bytes = new List<byte>();
        for (var i = 0; i < encoded.Length; i += 4)
        {
            Assert.AreEqual("\\x", encoded.Substring(i, 2));
            bytes.Add(Convert.ToByte(encoded.Substring(i + 2, 2), 16));
        }
        Assert.AreEqual(value, new UTF8Encoding(false, true).GetString(bytes.ToArray()));
    }

    [TestMethod]
    public void InvalidUtf16CannotSilentlyChangeBucketIdentity()
        => Assert.ThrowsException<EncoderFallbackException>(() => InfluxDBCqlCompiler.FluxStringLiteral("\ud800"));

    [TestMethod]
    public void NativeReadConstrainsIdsAndPreservesTiedHeadsForRejection()
    {
        var id = Guid.NewGuid();
        var sql = InfluxDBCqlCompiler.BuildNativeFlux("${die()}", new[] { id });
        Assert.IsFalse(sql.Contains("${"));
        StringAssert.Contains(sql, "r.id == " + InfluxDBCqlCompiler.FluxStringLiteral(id.ToString("D")));
        StringAssert.Contains(sql, "max(column: \"_time\")");
        StringAssert.Contains(sql, "on: [\"id\", \"_time\"]");
        Assert.ThrowsException<ArgumentException>(() => InfluxDBCqlCompiler.BuildNativeFlux("bucket", Array.Empty<Guid>()));
        Assert.ThrowsException<ArgumentException>(() => InfluxDBCqlCompiler.BuildNativeFlux("bucket", new[] { id, id }));
        Assert.ThrowsException<ArgumentException>(() => InfluxDBCqlCompiler.BuildNativeFlux("bucket", Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray()));
    }
}
