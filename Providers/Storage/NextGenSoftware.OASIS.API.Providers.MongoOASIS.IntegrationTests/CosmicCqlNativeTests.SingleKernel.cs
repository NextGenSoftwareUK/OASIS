using System;
using System.Collections.Generic;
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

            var pipeline = new[]
            {
                new BsonDocument("$project", new BsonDocument { { "_id", 0 },
                    { "inputs", new BsonDocument("$literal", new BsonArray(vectors)) } }),
                new BsonDocument("$unwind", new BsonDocument { { "path", "$inputs" }, { "includeArrayIndex", "ordinal" } }),
                new BsonDocument("$project", new BsonDocument { { "ordinal", 1 },
                    { "normalized", Translator.SingleFromNativeDouble("$inputs") } }),
                new BsonDocument("$sort", new BsonDocument("ordinal", 1))
            };
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
}
