using System.Numerics;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public async Task NativeDecimalAggregateKernelMatchesExactRounding()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var session = driver.AsyncSession();
        using var transaction = await session.BeginTransactionAsync(c => c.WithTimeout(TimeSpan.FromSeconds(30)));
            var packets = new List<(long[] Parts, long Count)>();
            void Add(params decimal[] values)
            {
                var words = new long[7];
                foreach (var value in values)
                {
                    var next = DecimalWordsFromClrBits(value);
                    for (var part = 0; part < words.Length; part++) words[part] = checked(words[part] + next[part]);
                }
                packets.Add((words, values.Length));
            }
            const decimal unit = 0.0000000000000000000000000001m;
            Add(); Add(0m); Add(unit, 0m); Add(3m * unit, 0m); Add(-3m * unit, 0m);
            Add(decimal.MaxValue, decimal.MaxValue); Add(decimal.MinValue, decimal.MinValue);
            Add(decimal.MaxValue, decimal.MaxValue, decimal.MinValue, decimal.MinValue, unit);
            Add(decimal.MaxValue, decimal.MaxValue - 1m); Add(decimal.MaxValue, -0.1m);
            Add(decimal.MinValue, 0.1m); Add(7.9228162514264337593543950335m, unit);

            // Rounding a too-wide candidate before reducing scale would select
            // the wrong adjacent representable Decimal for these13 inputs.
            var boundary = Enumerable.Repeat(7.922816251426433759354395035m, 7)
                .Concat(Enumerable.Repeat(7.922816251426433759354395036m, 6)).ToArray();
            Add(boundary); Add(boundary.Select(value => -value).ToArray());
            for (byte scale = 0; scale <= 28; scale++)
            {
                var maximum = new decimal(-1, -1, -1, false, scale);
                Add(maximum, maximum); Add(-maximum, -maximum);
            }
            var random = new Random(20261025);
            int Word() => unchecked((int)(uint)random.NextInt64(0, (long)uint.MaxValue + 1));
            for (var sample = 0; sample < 64; sample++)
                Add(Enumerable.Range(0, 2 + sample % 4).Select(_ => new decimal(
                    Word(), Word(), Word(), random.Next(2) == 0, (byte)random.Next(29))).ToArray());
            packets.Add((DecimalWordsFromClrBits(decimal.MaxValue).Select(word => checked(word * int.MaxValue)).ToArray(), int.MaxValue));
            packets.Add((DecimalWordsFromClrBits(decimal.MinValue).Select(word => checked(word * int.MaxValue)).ToArray(), int.MaxValue));


        var inputs = packets.Select((packet, index) => new Dictionary<string, object>
        {
            ["id"] = (long)index, ["parts"] = packet.Parts, ["count"] = packet.Count
        }).ToArray();
        var query = "UNWIND $inputs AS packet RETURN packet.id AS id,"
            + Provider.DecimalAggregateKey("packet.parts", "packet.count", false) + " AS sum,"
            + Provider.DecimalAggregateKey("packet.parts", "packet.count", true) + " AS mean ORDER BY id";
        var rows = await (await transaction.RunAsync(query, new { inputs })).ToListAsync();
        Assert.AreEqual(packets.Count, rows.Count);
        for (var index = 0; index < packets.Count; index++)
        {
            var packet = packets[index];
            foreach (var operation in new[] { CosmicScalarOperation.Sum, CosmicScalarOperation.Average })
            {
                var expected = CosmicDecimalScalarResults.FromParts(operation, typeof(decimal?), packet.Parts, packet.Count);
                var actual = rows[index][operation == CosmicScalarOperation.Sum ? "sum" : "mean"];
                if (expected.IsError || expected.Result == null)
                {
                    if (expected.IsError) Assert.AreEqual("CQL_SCALAR_OVERFLOW", expected.ErrorCode);
                    Assert.IsNull(actual, "Packet " + index);
                }
                else Assert.AreEqual(CosmicNumericScalarResults.DecimalOrderKey((decimal)expected.Result), actual, "Packet " + index);
            }
        }
        // This kernel only evaluates parameters; it never creates graph data.
        await transaction.RollbackAsync();
    }

    [TestMethod]
    public async Task NativeDecimalAggregateKernelDecodesSignedScaledKeys()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var values = new List<decimal> { 0m, 1m, -1m, 0.1m, -0.1m, new decimal(0,0,0,true,28) };
        for (byte scale = 0; scale <= 28; scale++)
        {
            values.Add(new decimal(-1,-1,-1,false,scale));
            values.Add(new decimal(-1,-1,-1,true,scale));
        }
        var random = new Random(20261025);
        int Word() => unchecked((int)(uint)random.NextInt64(0, (long)uint.MaxValue + 1));
        for (var sample = 0; sample < 2048; sample++)
            values.Add(new decimal(Word(), Word(), Word(), random.Next(2)==0, (byte)(sample%29)));
        var inputs = values.Select((value,index) => new Dictionary<string,object>
        {
            ["id"]=(long)index, ["key"]=CosmicNumericScalarResults.DecimalOrderKey(value)
        }).ToArray();
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var session = driver.AsyncSession();
        using var transaction = await session.BeginTransactionAsync(c => c.WithTimeout(TimeSpan.FromSeconds(30)));
        var rows = await (await transaction.RunAsync("UNWIND $inputs AS packet RETURN packet.id AS id,"
            + Provider.DecimalAggregateParts("packet.key") + " AS parts ORDER BY id", new { inputs })).ToListAsync();
        Assert.AreEqual(values.Count, rows.Count);
        for (var index=0; index<values.Count; index++)
            CollectionAssert.AreEqual(DecimalWordsFromClrBits(values[index]), rows[index]["parts"].As<List<long>>().ToArray(), "Input " + index);
        await transaction.RollbackAsync();
    }

    private static long[] DecimalWordsFromClrBits(decimal value)
    {
        var bits = decimal.GetBits(value);
        var units = (new BigInteger((uint)bits[0]) | new BigInteger((uint)bits[1]) << 32 | new BigInteger((uint)bits[2]) << 64)
            * BigInteger.Pow(10, 28 - ((bits[3] >> 16) & 255));
        var words = new long[7];
        for (var index = 6; index >= 0; index--)
        {
            units = BigInteger.DivRem(units, 1000000000, out var part);
            words[index] = value < 0 ? -(long)part : (long)part;
        }
        Assert.AreEqual(BigInteger.Zero, units);
        return words;
    }
}
