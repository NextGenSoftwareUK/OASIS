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
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task NativeFloatingPartsMatchIndependentIeeeFields(bool single)
        => WithMongoGraph(async fixture =>
        {
            var values = new List<BsonValue>();
            void Add(double value) { values.Add(new BsonDouble(value)); }
            Add(0d); Add(-0d); Add(single ? float.Epsilon : double.Epsilon);
            Add(single ? float.MaxValue : double.MaxValue);
            Add(single ? float.MinValue : double.MinValue);
            Add(single ? (double)0.1f : 0.1d);
            // Every exponent and both signs, including the subnormal boundary.
            for (var exponent = single ? -149 : -1074; exponent <= (single ? 127 : 1023); exponent++)
            {
                var value = Math.Pow(2, exponent);
                Add(value); Add(-value);
            }
            var random = new Random(20261025);
            var bytes = new byte[8];
            for (var accepted = 0; accepted < 512;)
            {
                random.NextBytes(bytes);
                var value = single ? (double)BitConverter.ToSingle(bytes, 0) : BitConverter.ToDouble(bytes, 0);
                if (!double.IsFinite(value)) continue;
                Add(value); accepted++;
            }

            var rows = await fixture.Records.Aggregate<BsonDocument>(
                SingleKernelPipeline(values, Translator.FloatingScaledParts("$inputs", single)),
                new AggregateOptions { MaxTime = TimeSpan.FromSeconds(30), BatchSize = 64, AllowDiskUse = false }).ToListAsync();
            Assert.Equal(values.Count, rows.Count);
            for (var row = 0; row < values.Count; row++)
            {
                var value = values[row].AsDouble;
                var raw = single ? (ulong)(uint)BitConverter.SingleToInt32Bits((float)value)
                    : (ulong)BitConverter.DoubleToInt64Bits(value);
                var fractionBits = single ? 23 : 52;
                var exponentMask = single ? 255UL : 2047UL;
                var exponent = (int)((raw >> fractionBits) & exponentMask);
                var fraction = raw & ((1UL << fractionBits) - 1);
                var magnitude = new BigInteger(exponent == 0 ? fraction : fraction | (1UL << fractionBits));
                if (exponent > 0) magnitude <<= exponent - 1;
                var actual = rows[row]["normalized"].AsBsonArray;
                Assert.Equal(single ? 10 : 70, actual.Count);
                for (var part = actual.Count - 1; part >= 0; part--)
                {
                    var expected = (long)(magnitude & ((BigInteger.One << 30) - 1));
                    magnitude >>= 30;
                    if (value < 0) expected = -expected;
                    Assert.True(actual[part].IsInt64, "Each native digit must retain Int64 arithmetic.");
                    Assert.Equal(expected, actual[part].AsInt64);
                }
                Assert.Equal(BigInteger.Zero, magnitude);
            }
            output.WriteLine("Verified floating digit vectors: " + values.Count);
        }, width: 0);
}
