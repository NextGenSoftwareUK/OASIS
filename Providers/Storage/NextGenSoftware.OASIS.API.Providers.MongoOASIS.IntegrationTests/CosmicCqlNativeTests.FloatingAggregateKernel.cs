using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using Translator = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoCqlTranslator;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task NativeFloatingAggregateValuesMatchExactCoreRounding(bool single)
        => WithMongoGraph(async fixture =>
        {
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

            var vectors = packets.Select(packet => (BsonValue)new BsonDocument
            {
                { "parts", new BsonArray(packet.Parts.Select(word => new BsonInt64(word))) }, { "count", packet.Count }
            }).ToArray();
            var expression = new BsonDocument
            {
                { "sum", Translator.FloatingAggregateValue("$inputs.parts", "$inputs.count", false, single) },
                { "mean", Translator.FloatingAggregateValue("$inputs.parts", "$inputs.count", true, single) }
            };
            var rows = await fixture.Records.Aggregate<BsonDocument>(SingleKernelPipeline(vectors, expression),
                new AggregateOptions { MaxTime = TimeSpan.FromSeconds(30), BatchSize = 64, AllowDiskUse = false }).ToListAsync();
            Assert.Equal(packets.Count, rows.Count);
            for (var index = 0; index < packets.Count; index++)
            {
                var packet = packets[index]; var observed = rows[index]["normalized"].AsBsonDocument;
                foreach (var operation in new[] { CosmicScalarOperation.Sum, CosmicScalarOperation.Average })
                {
                    var expected = CosmicFloatingAggregateResults.FromParts(operation,
                        single ? typeof(float?) : typeof(double?), packet.Parts, packet.Count);
                    var actual = observed[operation == CosmicScalarOperation.Sum ? "sum" : "mean"];
                    if (expected.IsError || expected.Result == null)
                    {
                        if (expected.IsError) Assert.Equal("CQL_SCALAR_OVERFLOW", expected.ErrorCode);
                        Assert.True(actual.IsBsonNull, "Expected empty/overflow for packet " + index + ": " + actual);
                    }
                    else
                    {
                        var exact = single ? (double)(float)expected.Result : (double)expected.Result;
                        Assert.True(actual.IsDouble, "Expected binary result for packet " + index + ": " + actual);
                        Assert.Equal(BitConverter.DoubleToInt64Bits(exact), BitConverter.DoubleToInt64Bits(actual.AsDouble));
                    }
                }
            }
            output.WriteLine("Verified native floating normalization packets: " + packets.Count);
        }, width: 0);
}
