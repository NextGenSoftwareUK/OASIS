using System.Numerics;
using Neo4j.Driver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task NativeFloatingPartsMatchIndependentIeeeFields(bool single)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var values = new List<double>();
        values.AddRange(new[] { 0d, -0d, single ? (double)float.Epsilon : double.Epsilon,
            single ? (double)float.MaxValue : double.MaxValue, single ? (double)float.MinValue : double.MinValue,
            single ? (double)0.1f : 0.1d });
        for (var exponent = single ? -149 : -1074; exponent <= (single ? 127 : 1023); exponent++)
        {
            values.Add(Math.Pow(2, exponent)); values.Add(-Math.Pow(2, exponent));
        }
        var random = new Random(20261025); var bytes = new byte[8];
        for (var accepted = 0; accepted < 512;)
        {
            random.NextBytes(bytes);
            var value = single ? (double)BitConverter.ToSingle(bytes, 0) : BitConverter.ToDouble(bytes, 0);
            if (!double.IsFinite(value)) continue;
            values.Add(value); accepted++;
        }
        var inputs = values.Select((value, index) => new Dictionary<string, object>
            { ["id"] = (long)index, ["value"] = value }).ToArray();
        var parameters = new Dictionary<string, object> { ["inputs"] = inputs };
        string Bind(object value)
        {
            var name = "cfParameter" + parameters.Count;
            parameters.Add(name, value); return "$" + name;
        }
        var expression = Provider.FloatingAggregateParts("packet.value", single, Bind);
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var session = driver.AsyncSession();
        using var transaction = await session.BeginTransactionAsync(c => c.WithTimeout(TimeSpan.FromSeconds(30)));
        var rows = await (await transaction.RunAsync("UNWIND $inputs AS packet RETURN packet.id AS id,"
            + expression + " AS parts ORDER BY id", parameters)).ToListAsync();
        Assert.AreEqual(values.Count, rows.Count);
        for (var index = 0; index < values.Count; index++)
        {
            // Decode IEEE fields independently of the server's remainder and
            // power-of-two division expression. Each published word is Int64.
            var value = values[index];
            var raw = single ? (ulong)(uint)BitConverter.SingleToInt32Bits((float)value)
                : (ulong)BitConverter.DoubleToInt64Bits(value);
            var bits = single ? 23 : 52;
            var exponent = (int)((raw >> bits) & (single ? 255UL : 2047UL));
            var fraction = raw & ((1UL << bits) - 1);
            var magnitude = new BigInteger(exponent == 0 ? fraction : fraction | (1UL << bits));
            if (exponent > 0) magnitude <<= exponent - 1;
            var expected = new long[single ? 10 : 70];
            for (var part = expected.Length - 1; part >= 0; part--)
            {
                var digit = (long)(magnitude & ((BigInteger.One << 30) - 1)); magnitude >>= 30;
                expected[part] = value < 0 ? -digit : digit;
            }
            Assert.AreEqual(BigInteger.Zero, magnitude);
            var actual = rows[index]["parts"] as IReadOnlyList<object>;
            Assert.IsNotNull(actual, "Native words must be an array.");
            Assert.IsTrue(actual!.All(word => word is long), "Native words must retain Int64 types.");
            CollectionAssert.AreEqual(expected, actual.Cast<long>().ToArray(), "Input " + index);
        }
        // This component evaluates only bound values; no graph data is created.
        await transaction.RollbackAsync();
        Console.WriteLine("Verified native floating digit vectors: " + values.Count);
    }
}
