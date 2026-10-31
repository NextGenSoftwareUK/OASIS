using System;
using System.Collections.Generic;
using System.Linq;
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
    public Task NativeDecimalAggregateValuesMatchCoreRoundingAndOverflow()
        => WithMongoGraph(async fixture =>
        {
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

            var vectors = packets.Select(packet => (BsonValue)new BsonDocument
            {
                { "parts", new BsonArray(packet.Parts.Select(word => new BsonDecimal128(new Decimal128(word)))) },
                { "count", packet.Count }
            }).ToArray();
            var expression = new BsonDocument
            {
                { "sum", Translator.DecimalAggregateValue("$inputs.parts", "$inputs.count", average: false) },
                { "mean", Translator.DecimalAggregateValue("$inputs.parts", "$inputs.count", average: true) }
            };
            var rows = await fixture.Records.Aggregate<BsonDocument>(SingleKernelPipeline(vectors, expression),
                new AggregateOptions { MaxTime = TimeSpan.FromSeconds(30), BatchSize = 64, AllowDiskUse = false }).ToListAsync();
            Assert.Equal(packets.Count, rows.Count);
            for (var index = 0; index < packets.Count; index++)
            {
                var packet = packets[index]; var observed = rows[index]["normalized"].AsBsonDocument;
                foreach (var operation in new[] { CosmicScalarOperation.Sum, CosmicScalarOperation.Average })
                {
                    var expected = CosmicDecimalScalarResults.FromParts(operation, typeof(decimal?), packet.Parts, packet.Count);
                    var actual = observed[operation == CosmicScalarOperation.Sum ? "sum" : "mean"];
                    if (expected.IsError || expected.Result == null)
                    {
                        if (expected.IsError) Assert.Equal("CQL_SCALAR_OVERFLOW", expected.ErrorCode);
                        Assert.True(actual.IsBsonNull, "Expected empty/overflow for packet " + index + ": " + actual);
                    }
                    else
                    {
                        Assert.True(actual.IsDecimal128, "Expected exact Decimal128 for packet " + index + ": " + actual);
                        Assert.Equal(0, Decimal128.Compare(new Decimal128((decimal)expected.Result), actual.AsDecimal128));
                    }
                }
            }
            output.WriteLine("Verified native Decimal final normalization packets: " + packets.Count);
        }, width: 0);
}
