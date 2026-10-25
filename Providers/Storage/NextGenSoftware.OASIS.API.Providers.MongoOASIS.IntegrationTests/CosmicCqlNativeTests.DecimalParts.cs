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
    [Fact]
    public Task NativeDecimalScaledPartsMatchEveryClrScaleAndReduction()
        => WithMongoGraph(async fixture =>
        {
            var values = new List<decimal> { 0m, 1m, -1m, 0.1m, -0.1m,
                0.0000000000000000000000000001m, -0.0000000000000000000000000001m,
                new decimal(0, 0, 0, true, 28) };
            for (byte scale = 0; scale <= 28; scale++)
            {
                values.Add(new decimal(-1, -1, -1, false, scale));
                values.Add(new decimal(-1, -1, -1, true, scale));
            }
            var random = new Random(20261025);
            int Word() => unchecked((int)(uint)random.NextInt64(0, (long)uint.MaxValue + 1));
            for (var sample = 0; sample < 2048; sample++)
                values.Add(new decimal(Word(), Word(), Word(), random.Next(2) == 0, (byte)(sample % 29)));

            var vectors = values.Select(value => (BsonValue)new BsonDecimal128(new Decimal128(value))).ToArray();
            var pipeline = SingleKernelPipeline(vectors, Translator.DecimalScaledParts("$inputs"));
            var options = new AggregateOptions { MaxTime = TimeSpan.FromSeconds(30), BatchSize = 128, AllowDiskUse = false };
            var rows = await fixture.Records.Aggregate<BsonDocument>(pipeline, options).ToListAsync();
            Assert.Equal(values.Count, rows.Count);
            var expectedTotals = new long[7];
            for (var index = 0; index < values.Count; index++)
            {
                var expected = DecimalWordsFromClrBits(values[index]);
                var observed = rows[index]["normalized"].AsBsonArray;
                Assert.Equal(7, observed.Count);
                for (var part = 0; part < 7; part++)
                {
                    Assert.True(observed[part].IsDecimal128);
                    Assert.Equal(new Decimal128(expected[part]), observed[part].AsDecimal128);
                    expectedTotals[part] = checked(expectedTotals[part] + expected[part]);
                }
                var decoded = CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum,
                    typeof(decimal), observed.Select(value => value.ToInt64()).ToArray(), 1);
                Assert.False(decoded.IsError, decoded.Message);
                Assert.Equal(values[index], decoded.Result);
            }

            // A real server reduction must retain the seven exact signed sums;
            // never recombine the complete scaled numerator into Decimal128.
            var reduction = new BsonDocument { { "_id", BsonNull.Value }, { "count", new BsonDocument("$sum", 1L) } };
            for (var part = 0; part < 7; part++)
                reduction.Add("p" + part, new BsonDocument("$sum",
                    new BsonDocument("$arrayElemAt", new BsonArray { "$normalized", part })));
            var reduced = await fixture.Records.Aggregate<BsonDocument>(
                pipeline.Concat(new[] { new BsonDocument("$group", reduction) }).ToArray(), options).SingleAsync();
            Assert.Equal(values.Count, reduced["count"].ToInt64());
            for (var part = 0; part < 7; part++)
                Assert.Equal(new Decimal128(expectedTotals[part]), reduced["p" + part].AsDecimal128);
            output.WriteLine("Verified Decimal scaled-packet vectors and native reduction: " + values.Count);
        }, width: 0);

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
        Assert.Equal(BigInteger.Zero, units);
        return words;
    }
}
