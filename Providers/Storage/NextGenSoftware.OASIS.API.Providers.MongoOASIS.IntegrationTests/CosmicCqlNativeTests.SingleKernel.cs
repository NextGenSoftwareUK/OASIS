using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Translator = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoCqlTranslator;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Fact]
    public Task NativeSingleDoubleKernelMatchesFiniteBinary32RoundingAndInvalidDomains()
        => WithMongoGraph(async fixture =>
        {
            var vectors = new List<BsonValue>();
            void Add(double value) => vectors.Add(new BsonDouble(value));
            var overflow = (double)float.MaxValue + Math.Pow(2, 103);
            foreach (var value in new[] { double.NaN, double.NegativeInfinity, double.PositiveInfinity,
                -overflow, Math.BitIncrement(-overflow), Math.BitDecrement(overflow), overflow,
                double.MinValue, double.MaxValue, -double.Epsilon, double.Epsilon,
                -(double)float.Epsilon, (double)float.Epsilon, -0d, 0d, 0.1d,
                Math.Pow(2, -150), Math.BitIncrement(Math.Pow(2, -150)) }) Add(value);

            var random = new Random(20261025);
            var bytes = new byte[8];
            for (var accepted = 0; accepted < 8192;)
            {
                random.NextBytes(bytes);
                var value = BitConverter.ToDouble(bytes, 0);
                if (double.IsNaN(value) || double.IsInfinity(value)) continue;
                Add(value); accepted++;
            }
            for (var accepted = 0; accepted < 1024;)
            {
                random.NextBytes(bytes);
                var value = BitConverter.ToSingle(bytes, 0);
                var next = MathF.BitIncrement(value);
                if (float.IsNaN(value) || float.IsInfinity(value) || float.IsInfinity(next)) continue;
                Add(value);
                var midpoint = ((double)value + next) / 2d;
                Add(Math.BitDecrement(midpoint)); Add(midpoint); Add(Math.BitIncrement(midpoint));
                accepted++;
            }
            vectors.AddRange(new BsonValue[] { BsonNull.Value, new BsonString("0.1"), new BsonInt32(0),
                new BsonInt64(long.MaxValue), new BsonDecimal128(0.1m), new BsonBoolean(true), new BsonDocument() });

            var pipeline = SingleKernelPipeline(vectors, Translator.SingleFromNativeDouble("$inputs"));
            var rows = await fixture.Records.Aggregate<BsonDocument>(pipeline,
                new AggregateOptions { MaxTime = TimeSpan.FromSeconds(30), BatchSize = 128, AllowDiskUse = false }).ToListAsync();
            Assert.Equal(vectors.Count, rows.Count);
            for (var index = 0; index < vectors.Count; index++)
            {
                var input = vectors[index]; var observed = rows[index]["normalized"];
                var narrowed = input.IsDouble ? (float)input.AsDouble : float.NaN;
                var valid = input.IsDouble && !float.IsNaN(narrowed) && !float.IsInfinity(narrowed)
                    && (narrowed != 0f || input.AsDouble == 0d);
                if (!valid) Assert.True(observed.IsBsonDocument && observed.AsBsonDocument.ElementCount == 0,
                    "Invalid native Single input: " + input);
                else
                {
                    Assert.True(observed.IsDouble, "Native Single output must be widened binary32: " + input);
                    var expected = narrowed == 0f ? 0d : (double)narrowed;
                    Assert.True(BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(observed.AsDouble),
                        "Native Single rounding differs for " + input + ": " + observed);
                }
            }
            output.WriteLine("Verified native Single binary64 component vectors: " + vectors.Count);
        }, width: 0);
    [Fact]
    public Task NativeSingleIntegerKernelRetainsInt64HalfwayDirection()
        => WithMongoGraph(async fixture =>
        {
            var vectors = new List<BsonValue>();
            void Add(long value) => vectors.Add(new BsonInt64(value));
            foreach (var value in new[] { long.MinValue, long.MaxValue, -16777219L, -16777217L,
                -1L, 0L, 1L, 16777217L, 16777219L, 9007199254740993L }) Add(value);
            for (var exponent = 24; exponent <= 62; exponent++)
                foreach (var odd in new[] { 1L, 3L })
                {
                    var midpoint = (1L << exponent) + odd * (1L << (exponent - 24));
                    foreach (var offset in new[] { -1L, 0L, 1L })
                    { Add(midpoint + offset); Add(-(midpoint + offset)); }
                }
            var random = new Random(20261026);
            var bytes = new byte[8];
            for (var index = 0; index < 8192; index++)
            { random.NextBytes(bytes); Add(BitConverter.ToInt64(bytes, 0)); }
            for (var index = 0; index < 1024; index++)
                vectors.Add(new BsonInt32((int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1)));
            vectors.AddRange(new BsonValue[] { BsonNull.Value, new BsonString("1"), new BsonDouble(1d),
                new BsonDecimal128(1m), new BsonBoolean(true), new BsonDocument() });
            var rows = await fixture.Records.Aggregate<BsonDocument>(
                SingleKernelPipeline(vectors, Translator.SingleFromNativeInteger("$inputs")),
                new AggregateOptions { MaxTime = TimeSpan.FromSeconds(30), BatchSize = 128, AllowDiskUse = false }).ToListAsync();
            Assert.Equal(vectors.Count, rows.Count);
            for (var index = 0; index < vectors.Count; index++)
            {
                var input = vectors[index]; var observed = rows[index]["normalized"];
                if (!input.IsInt32 && !input.IsInt64)
                    Assert.True(observed.IsBsonDocument && observed.AsBsonDocument.ElementCount == 0);
                else
                {
                    var expected = RoundedIntegerSingle(input.ToInt64());
                    Assert.True(observed.IsDouble);
                    Assert.True(BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(observed.AsDouble),
                        "Native integer Single rounding differs for " + input + ": " + observed);
                }
            }
            output.WriteLine("Verified native Single integer component vectors: " + vectors.Count);
        }, width: 0);

    // Independent exact integer reference: decide the tie before any floating
    // conversion. The rounded result has24significant bits and widens exactly.
    private static double RoundedIntegerSingle(long value)
    {
        var magnitude = BigInteger.Abs(new BigInteger(value));
        var shift = (int)magnitude.GetBitLength() - 24;
        if (shift <= 0) return (double)value;
        var quotient = magnitude >> shift;
        var remainder = magnitude - (quotient << shift);
        var half = BigInteger.One << (shift - 1);
        if (remainder > half || remainder == half && !quotient.IsEven) quotient++;
        var rounded = quotient << shift;
        return (double)(value < 0 ? -rounded : rounded);
    }

    private static BsonDocument[] SingleKernelPipeline(IReadOnlyCollection<BsonValue> vectors, BsonValue expression)
        => new[]
        {
            new BsonDocument("$project", new BsonDocument { { "_id", 0 },
                { "inputs", new BsonDocument("$literal", new BsonArray(vectors)) } }),
            new BsonDocument("$unwind", new BsonDocument { { "path", "$inputs" }, { "includeArrayIndex", "ordinal" } }),
            new BsonDocument("$project", new BsonDocument { { "ordinal", 1 }, { "normalized", expression } }),
            new BsonDocument("$sort", new BsonDocument("ordinal", 1))
        };

}
