using System;
using System.Collections.Generic;
using System.Globalization;
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
    public Task NativeSingleDecimalKernelPreservesOriginalHalfwayDirection()
        => WithMongoGraph(async fixture =>
        {
            var vectors = new List<BsonValue>();
            void Add(string text) => vectors.Add(new BsonDecimal128(Decimal128.Parse(text)));
            void Midpoint(long bits)
            {
                var exponent = (int)(bits / 8388608);
                var coefficient = new BigInteger(2 * (bits % 8388608 + (exponent == 0 ? 0 : 8388608)) + 1);
                var binaryScale = exponent == 0 ? -150 : exponent - 151;
                var numerator = coefficient * BigInteger.Pow(binaryScale < 0 ? 5 : 2, Math.Abs(binaryScale));
                var digits = numerator.ToString(CultureInfo.InvariantCulture);
                var decimalScale = binaryScale < 0 ? binaryScale : 0;
                var prefix = digits.Length > 34 ? BigInteger.Parse(digits.Substring(0, 34), CultureInfo.InvariantCulture)
                    : numerator * BigInteger.Pow(10, 34 - digits.Length);
                decimalScale += digits.Length - 34;
                foreach (var offset in new[] { -1, 0, 1 })
                {
                    var text = (prefix + offset).ToString(CultureInfo.InvariantCulture) + "E" + decimalScale.ToString(CultureInfo.InvariantCulture);
                    Add(text); Add("-" + text);
                }
            }
            foreach (var bits in new[] { 0L, 1L, 0x007fffffL, 0x00800000L, 0x3f800000L,
                0x3f800001L, 0x40000000L, 0x40400000L, 0x7f7ffffeL, 0x7f7fffffL }) Midpoint(bits);
            var random = new Random(20261027);
            for (var index = 0; index < 256; index++) Midpoint(random.NextInt64(0, 0x7f800000L));
            for (var index = 0; index < 256; index++)
            {
                var digits = new char[34]; digits[0] = (char)('1' + random.Next(9));
                for (var digit = 1; digit < digits.Length; digit++) digits[digit] = (char)('0' + random.Next(10));
                Add((index % 2 == 0 ? "" : "-") + new string(digits) + "E" + random.Next(-80, 41).ToString(CultureInfo.InvariantCulture));
            }
            foreach (var text in new[] { "0", "-0", "0E-6176", "-0E-6176", "0.1", "-0.1", "1E-6176", "-1E-6176",
                "1E1000", "-1E1000", "NaN", "Infinity", "-Infinity", new string('9', 34) + "E6111" }) Add(text);
            vectors.AddRange(new BsonValue[] { BsonNull.Value, new BsonString("0.1"), new BsonInt32(1),
                new BsonInt64(long.MaxValue), new BsonDouble(0.1d), new BsonBoolean(true), new BsonDocument() });
            var rows = await fixture.Records.Aggregate<BsonDocument>(
                SingleKernelPipeline(vectors, Translator.SingleFromNativeDecimal("$inputs")),
                new AggregateOptions { MaxTime = TimeSpan.FromSeconds(30), BatchSize = 64, AllowDiskUse = false }).ToListAsync();
            Assert.Equal(vectors.Count, rows.Count);
            for (var index = 0; index < vectors.Count; index++)
            {
                var input = vectors[index]; var observed = rows[index]["normalized"];
                var valid = input.IsDecimal128 && !Decimal128.IsNaN(input.AsDecimal128) && !Decimal128.IsInfinity(input.AsDecimal128);
                var narrowed = valid ? float.Parse(input.AsDecimal128.ToString(), CultureInfo.InvariantCulture) : float.NaN;
                valid = valid && !float.IsNaN(narrowed) && !float.IsInfinity(narrowed)
                    && (narrowed != 0f || input.AsDecimal128.CompareTo(new Decimal128(0)) == 0);
                if (!valid) Assert.True(observed.IsBsonDocument && observed.AsBsonDocument.ElementCount == 0,
                    "Invalid Decimal128 Single input: " + input);
                else
                {
                    Assert.True(observed.IsDouble);
                    var expected = narrowed == 0f ? 0d : (double)narrowed;
                    Assert.True(BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(observed.AsDouble),
                        "Native Decimal128 Single rounding differs for " + input + ": " + observed);
                }
            }
            output.WriteLine("Verified native Single Decimal128 component vectors: " + vectors.Count);
        }, width: 0);
}
