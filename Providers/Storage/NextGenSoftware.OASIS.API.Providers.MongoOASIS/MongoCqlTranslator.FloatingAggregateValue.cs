using System;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    internal static BsonValue FloatingAggregateValue(BsonValue parts, BsonValue count, bool average, bool single)
        => new FloatingAggregateCompiler(single).Compile(parts, count, average);

    // Only exact integer limb arithmetic uses Decimal128. The original binary
    // values and the complete large numerator never pass through Decimal128.
    private sealed class FloatingAggregateCompiler
    {
        private readonly int width;
        private readonly int precision;
        private readonly int maximumShift;
        private readonly int minimumExponent;
        private int sequence;

        internal FloatingAggregateCompiler(bool single)
        {
            width = single ? CosmicFloatingAggregateResults.SingleLimbCount : CosmicFloatingAggregateResults.DoubleLimbCount;
            precision = single ? 24 : 53;
            maximumShift = single ? 253 : 2045;
            minimumExponent = single ? -149 : -1074;
        }

        private static BsonValue D(decimal number) => Literal(new BsonDecimal128(new Decimal128(number)));
        private static BsonValue F(string variable, string name) => variable + "." + name;
        private static BsonValue At(BsonValue array, BsonValue index) => Op("$arrayElemAt", array, index);
        private static BsonValue Truncate(BsonValue value) => new BsonDocument("$trunc", new BsonArray { value, Literal(0) });

        private BsonValue Let(BsonValue expression, Func<string, BsonValue> body)
        {
            var name = "cqlfloat" + sequence++;
            return new BsonDocument("$let", new BsonDocument
            {
                { "vars", new BsonDocument(name, expression) }, { "in", body("$$" + name) }
            });
        }

        private BsonValue Reduce(BsonValue input, BsonValue initial, Func<string, string, BsonValue> body)
            => new BsonDocument("$reduce", new BsonDocument
            {
                { "input", input }, { "initialValue", initial },
                { "in", Let("$$value", state => Let("$$this", item => body(state, item))) }
            });

        private BsonValue Normalize(BsonValue parts)
            => Reduce(new BsonDocument("$reverseArray", parts),
                new BsonDocument { { "carry", D(0) }, { "digits", new BsonArray() } },
                (state, word) => Let(Op("$add", word, F(state, "carry")), value =>
                Let(Op("$mod", value, D(1073741824m)), raw =>
                Let(Choose(Op("$lt", raw, D(0)), Op("$add", raw, D(1073741824m)), raw), digit =>
                    new BsonDocument
                    {
                        { "carry", Op("$divide", Op("$subtract", value, digit), D(1073741824m)) },
                        { "digits", Op("$concatArrays", new BsonArray { digit }, F(state, "digits")) }
                    }))));

        private BsonValue Divide(BsonValue words, BsonValue denominator)
            => Reduce(words, new BsonDocument { { "q", new BsonArray() }, { "r", D(0) } },
                (state, word) => Let(Op("$add", Op("$multiply", F(state, "r"), D(1073741824m)), word), value =>
                Let(Truncate(Op("$divide", value, denominator)), quotient => new BsonDocument
                {
                    { "q", Op("$concatArrays", F(state, "q"), new BsonArray { quotient }) },
                    { "r", Op("$subtract", value, Op("$multiply", quotient, denominator)) }
                })));

        private BsonValue Shift(BsonValue words)
            => Let(Reduce(Op("$range", Literal(0), Literal(width + 1)), Literal(-1),
                (first, index) => Choose(Op("$gte", first, Literal(0)), first,
                    Choose(Op("$gt", At(words, index), D(0)), index, Literal(-1)))), first =>
                Choose(Op("$lt", first, Literal(0)), Literal(0),
                    Let(At(words, first), leading =>
                    Let(Reduce(Op("$range", Literal(0), Literal(30)), Literal(0),
                        (bits, exponent) => Choose(Op("$gte", leading,
                            Op("$pow", D(2), exponent)), Op("$add", exponent, Literal(1)), bits)), bits =>
                        new BsonDocument("$max", new BsonArray { Literal(0),
                            Op("$subtract", Op("$add", Op("$multiply", Op("$subtract", Literal(width), first), Literal(30)), bits), Literal(precision)) })) )));

        private BsonValue Increment(BsonValue words, BsonValue shift, BsonValue coefficient,
            BsonValue remainder, BsonValue denominator)
        {
            var odd = Op("$ne", Op("$mod", coefficient, D(2)), D(0));
            return Choose(Op("$eq", shift, Literal(0)),
                Op("$or", Op("$gt", Op("$multiply", remainder, D(2)), denominator),
                    Op("$and", Op("$eq", Op("$multiply", remainder, D(2)), denominator), odd)),
                Let(Op("$subtract", shift, Literal(1)), guardPosition =>
                Let(Truncate(Op("$divide", guardPosition, D(30))), guardWords =>
                Let(Op("$subtract", Literal(width), guardWords), guardIndex =>
                Let(Op("$pow", D(2), Op("$mod", guardPosition, D(30))), guardPower =>
                Let(At(words, new BsonDocument("$toInt", guardIndex)), guardWord =>
                    Op("$and", Op("$eq", Op("$mod", Truncate(Op("$divide", guardWord, guardPower)), D(2)), D(1)),
                        Op("$or", odd, Op("$ne", remainder, D(0)), Op("$ne", Op("$mod", guardWord, guardPower), D(0)),
                            Reduce(Op("$range", new BsonDocument("$toInt", Op("$add", guardIndex, Literal(1))), Literal(width + 1)), Literal(false),
                                (sticky, index) => Op("$or", sticky, Op("$ne", At(words, index), D(0))))))))))));
        }

        private BsonValue Round(BsonValue words, BsonValue remainder, BsonValue denominator, BsonValue negative)
        {
            var powers = Literal(new BsonArray(Enumerable.Range(0, maximumShift + 1)
                .Select(shift => new BsonDouble(Math.Pow(2, minimumExponent + shift)))));
            return Let(Shift(words), shift =>
                Choose(Op("$gt", shift, Literal(maximumShift)), Literal(BsonNull.Value),
                    Let(Truncate(Op("$divide", shift, D(30))), removedWords =>
                    Let(Op("$mod", shift, D(30)), removedBits =>
                    Let(Reduce(new BsonDocument("$slice", new BsonArray { words, Literal(0),
                            new BsonDocument("$toInt", Op("$subtract", Literal(width + 1), removedWords)) }), D(0),
                        (prefix, word) => Op("$add", Op("$multiply", prefix, D(1073741824m)), word)), prefix =>
                    Let(Truncate(Op("$divide", prefix, Op("$pow", D(2), removedBits))), coefficient =>
                    Let(Op("$add", coefficient, Choose(Increment(words, shift, coefficient, remainder, denominator), D(1), D(0))), rounded =>
                        Choose(Op("$and", Op("$eq", shift, Literal(maximumShift)),
                                Op("$gt", rounded, D((decimal)(1L << precision) - 1))), Literal(BsonNull.Value),
                            Choose(Op("$eq", rounded, D(0)), Literal(0d),
                                Op("$multiply", Choose(negative, Literal(-1d), Literal(1d)),
                                    new BsonDocument("$toDouble", rounded),
                                    At(powers, new BsonDocument("$toInt", shift))))))))))));
        }

        internal BsonValue Compile(BsonValue parts, BsonValue count, bool average)
            => Let(parts, input => Let(count, participation =>
                Choose(Op("$and", Op(average ? "$gt" : "$gte", participation, D(0)),
                    Op("$lte", participation, Literal(CosmicNumericScalarResults.MaximumParticipatingValues))),
                    Let(Normalize(input), signed => Let(Op("$lt", F(signed, "carry"), D(0)), negative =>
                    Let(Choose(negative, Normalize(Reduce(input, new BsonArray(),
                        (state, word) => Op("$concatArrays", state, new BsonArray { Op("$multiply", word, D(-1)) }))), signed), magnitude =>
                    Let(Op("$concatArrays", new BsonArray { F(magnitude, "carry") }, F(magnitude, "digits")), words =>
                    Let(average ? (BsonValue)new BsonDocument("$toDecimal", participation) : D(1), denominator =>
                    Let(Divide(words, denominator), divided =>
                        Round(F(divided, "q"), F(divided, "r"), denominator, negative))))))),
                    Literal(BsonNull.Value))));
    }
}
