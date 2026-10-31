using System.Numerics;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [DataTestMethod]
    [DataRow(true)] [DataRow(false)]
    public async Task NativeFloatingAggregateKernelMatchesExactCoreRounding(bool single)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
            var width = single ? 10 : 70;
            var packets = new List<(long[] Parts, long Count)>();
            void Add(params double[] values)
            {
                var words = new long[width];
                foreach (var value in values)
                {
                    // Independent IEEE field decomposition, not the native
                    // remainder/division algorithm being verified.
                    var raw = single ? (ulong)(uint)BitConverter.SingleToInt32Bits((float)value)
                        : (ulong)BitConverter.DoubleToInt64Bits(value);
                    var fractionBits = single ? 23 : 52;
                    var exponent = (int)((raw >> fractionBits) & (single ? 255UL : 2047UL));
                    var fraction = raw & ((1UL << fractionBits) - 1);
                    var magnitude = new BigInteger(exponent == 0 ? fraction : fraction | (1UL << fractionBits));
                    if (exponent > 0) magnitude <<= exponent - 1;
                    for (var part = width - 1; part >= 0; part--)
                    {
                        var digit = (long)(magnitude & ((BigInteger.One << 30) - 1));
                        magnitude >>= 30;
                        words[part] = checked(words[part] + (value < 0 ? -digit : digit));
                    }
                }
                packets.Add((words, values.Length));
            }

            var unit = single ? (double)float.Epsilon : double.Epsilon;
            var maximum = single ? (double)float.MaxValue : double.MaxValue;
            Add(); Add(0d); Add(unit, 0d); Add(3d * unit, 0d); Add(-3d * unit, 0d);
            Add(maximum, maximum); Add(-maximum, -maximum);
            Add(maximum, maximum, -maximum, -maximum, unit);
            Add(maximum, unit); Add(-maximum, -unit);
            // The finite overflow midpoint is a rounding decision, not a
            // simple comparison with MaxValue. Test one exact unit either side.
            var overflowHalfQuantum = Math.Pow(2, single ? 103 : 970);
            Add(maximum, overflowHalfQuantum, -unit);
            Add(maximum, overflowHalfQuantum);
            Add(maximum, overflowHalfQuantum, unit);
            Add(-maximum, -overflowHalfQuantum, unit);
            Add(-maximum, -overflowHalfQuantum);
            Add(-maximum, -overflowHalfQuantum, -unit);
            Add(1d, single ? Math.Pow(2, -24) : Math.Pow(2, -53));
            Add(1d, single ? Math.Pow(2, -24) : Math.Pow(2, -53), unit);
            for (var exponent = single ? -149 : -1074; exponent <= (single ? 127 : 1023); exponent++)
            {
                var value = Math.Pow(2, exponent);
                Add(value); Add(-value);
                if (exponent % 30 == 0) Add(value, -value, unit);
            }
            var random = new Random(20261025); var bytes = new byte[8];
            for (var sample = 0; sample < 128; sample++)
            {
                var values = new List<double>();
                while (values.Count < 2 + sample % 5)
                {
                    random.NextBytes(bytes);
                    var value = single ? (double)BitConverter.ToSingle(bytes, 0) : BitConverter.ToDouble(bytes, 0);
                    if (double.IsFinite(value)) values.Add(value);
                }
                Add(values.ToArray());
            }
            Add(maximum);
            packets.Add((packets.Last().Parts.Select(word => checked(word * int.MaxValue)).ToArray(), int.MaxValue));
            packets.Add((packets.Last().Parts.Select(word => -word).ToArray(), int.MaxValue));

        var parameters = new Dictionary<string, object>();
        string Bind(object value)
        {
            var name = "cfParameter" + parameters.Count;
            parameters.Add(name, value); return "$" + name;
        }
        var query = "UNWIND $inputs AS packet RETURN packet.id AS id,"
            + Provider.FloatingAggregateValue("packet.parts", "packet.count", false, single, Bind) + " AS sum,"
            + Provider.FloatingAggregateValue("packet.parts", "packet.count", true, single, Bind) + " AS mean ORDER BY id";
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var session = driver.AsyncSession();
        // Bound each parameter-only kernel query below the production group cap.
        // This is a fixed test partition, never a retry after a failed query.
        for (var offset = 0; offset < packets.Count; offset += 256)
        {
            var inputs = packets.Skip(offset).Take(256).Select((packet, index) => new Dictionary<string, object>
            {
                ["id"] = (long)(offset + index), ["parts"] = packet.Parts, ["count"] = packet.Count
            }).ToArray();
            parameters["inputs"] = inputs;
            using var transaction = await session.BeginTransactionAsync(c => c.WithTimeout(TimeSpan.FromSeconds(30)));
            var rows = await (await transaction.RunAsync(query, parameters)).ToListAsync();
            Assert.AreEqual(inputs.Length, rows.Count);
            for (var index = 0; index < rows.Count; index++)
            {
                Assert.AreEqual((long)(offset + index), rows[index]["id"]);
                var packet = packets[offset + index];
                foreach (var operation in new[] { CosmicScalarOperation.Sum, CosmicScalarOperation.Average })
                {
                    var expected = CosmicFloatingAggregateResults.FromParts(operation,
                        single ? typeof(float?) : typeof(double?), packet.Parts, packet.Count);
                    var actual = rows[index][operation == CosmicScalarOperation.Sum ? "sum" : "mean"];
                    if (expected.IsError || expected.Result == null)
                    {
                        if (expected.IsError) Assert.AreEqual("CQL_SCALAR_OVERFLOW", expected.ErrorCode);
                        Assert.IsNull(actual, "Packet " + (offset + index));
                    }
                    else
                    {
                        var exact = single ? (double)(float)expected.Result : (double)expected.Result;
                        Assert.IsInstanceOfType(actual, typeof(double), "Native result must retain its floating type.");
                        Assert.AreEqual(BitConverter.DoubleToInt64Bits(exact), BitConverter.DoubleToInt64Bits((double)actual),
                            "Packet " + (offset + index) + ", operation " + operation);
                    }
                }
            }
            // No graph data is created or changed by these parameter kernels.
            await transaction.RollbackAsync();
        }
        Console.WriteLine("Verified native floating normalization packets: " + packets.Count);
    }
}
