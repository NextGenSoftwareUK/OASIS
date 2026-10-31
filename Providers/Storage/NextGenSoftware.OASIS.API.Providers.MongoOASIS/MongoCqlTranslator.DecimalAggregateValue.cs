using System;
using System.Linq;
using System.Numerics;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    internal static BsonValue DecimalAggregateValue(BsonValue parts, BsonValue count, bool average)
        => new DecimalAggregateCompiler().Compile(parts, count, average);

    // Eight base1e9 words retain the entire scaled numerator. Only a final
    // rounded96-bit coefficient is recombined into one Decimal128 value.
    private sealed class DecimalAggregateCompiler
    {
        private int sequence;
        private static BsonValue D(decimal value) => Literal(new BsonDecimal128(new Decimal128(value)));
        private static BsonValue At(BsonValue array, int index) => Op("$arrayElemAt", array, Literal(index));
        private static BsonValue Field(string variable, string field) => variable + "." + field;
        private static BsonValue Truncate(BsonValue value)
            => new BsonDocument("$trunc", new BsonArray { value, Literal(0) });

        private BsonValue Let(BsonValue expression, Func<string, BsonValue> body)
        {
            var name = "cqldec" + sequence++;
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

        private static BsonValue Words(BigInteger value)
        {
            var words = new BsonValue[8];
            for (var index = 7; index >= 0; index--)
            {
                value = BigInteger.DivRem(value, CosmicDecimalScalarResults.LimbBase, out var word);
                words[index] = new BsonInt64((long)word);
            }
            return Literal(new BsonArray(words));
        }

        private static BsonValue Greater(BsonValue first, BsonValue second)
            => Op("$or", Enumerable.Range(0, 8).Select(index => Op("$and",
                Enumerable.Range(0, index).Select(prior => (BsonValue)Op("$eq", At(first, prior), At(second, prior)))
                    .Concat(new[] { (BsonValue)Op("$gt", At(first, index), At(second, index)) }).ToArray())).Cast<BsonValue>().ToArray());

        private BsonValue Normalize(BsonValue parts)
            => Reduce(new BsonDocument("$reverseArray", parts), new BsonDocument
            {
                { "carry", D(0) }, { "digits", new BsonArray() }
            }, (state, word) => Let(Op("$add", word, Field(state, "carry")), value =>
                Let(Op("$mod", value, D(1000000000m)), raw =>
                Let(Choose(Op("$lt", raw, D(0)), Op("$add", raw, D(1000000000m)), raw), digit =>
                    new BsonDocument
                    {
                        { "carry", Op("$divide", Op("$subtract", value, digit), D(1000000000m)) },
                        { "digits", Op("$concatArrays", new BsonArray { digit }, Field(state, "digits")) }
                    }))));

        private BsonValue Divide(BsonValue words, BsonValue denominator)
            => Reduce(words, new BsonDocument { { "q", new BsonArray() }, { "r", D(0) } },
                (state, word) => Let(Op("$add", Op("$multiply", Field(state, "r"), D(1000000000m)), word), value =>
                Let(Truncate(Op("$divide", value, denominator)), quotient => new BsonDocument
                {
                    { "q", Op("$concatArrays", Field(state, "q"), new BsonArray { quotient }) },
                    { "r", Op("$subtract", value, Op("$multiply", quotient, denominator)) }
                })));

        private BsonValue Increment(BsonValue words)
            => Let(Reduce(new BsonDocument("$reverseArray", words),
                new BsonDocument { { "digits", new BsonArray() }, { "carry", D(1) } },
                (state, word) => Let(Op("$add", word, Field(state, "carry")), value => new BsonDocument
                {
                    { "digits", Op("$concatArrays", new BsonArray { Op("$mod", value, D(1000000000m)) }, Field(state, "digits")) },
                    { "carry", Truncate(Op("$divide", value, D(1000000000m))) }
                })), result => Field(result, "digits"));

        private BsonValue Round(BsonValue quotient, BsonValue remainder, BsonValue denominator)
        {
            var maximum = Words((BigInteger.One << 96) - 1);
            var initial = new BsonDocument
            {
                { "q", quotient }, { "last", D(0) }, { "sticky", Op("$ne", remainder, D(0)) },
                { "done", false }, { "chosen", BsonNull.Value }
            };
            return Reduce(Op("$range", Literal(0), Literal(29)), initial, (state, removed) =>
                Choose(Field(state, "done"), state,
                    Let(Choose(Op("$eq", removed, Literal(0)),
                        Op("$or", Op("$gt", Op("$multiply", remainder, D(2)), denominator),
                            Op("$and", Op("$eq", Op("$multiply", remainder, D(2)), denominator),
                                Op("$ne", Op("$mod", At(Field(state, "q"), 7), D(2)), D(0)))),
                        Op("$or", Op("$gt", Field(state, "last"), D(5)),
                            Op("$and", Op("$eq", Field(state, "last"), D(5)),
                                Op("$or", Field(state, "sticky"), Op("$ne", Op("$mod", At(Field(state, "q"), 7), D(2)), D(0)))))), increment =>
                    Let(Choose(increment, Increment(Field(state, "q")), Field(state, "q")), rounded =>
                        Choose(Greater(rounded, maximum),
                            Let(Divide(Field(state, "q"), D(10)), divided => new BsonDocument
                            {
                                { "q", Field(divided, "q") }, { "last", Field(divided, "r") },
                                { "sticky", Op("$or", Field(state, "sticky"), Op("$ne", Field(state, "last"), D(0))) },
                                { "done", false }, { "chosen", BsonNull.Value }
                            }),
                            new BsonDocument
                            {
                                { "q", Field(state, "q") }, { "last", Field(state, "last") }, { "sticky", Field(state, "sticky") },
                                { "done", true }, { "chosen", new BsonDocument
                                    { { "digits", rounded }, { "scale", Op("$subtract", Literal(28), removed) } } }
                            })))));
        }

        private BsonValue Publish(BsonValue chosen, BsonValue negative)
        {
            var factors = new BsonArray(); var power = 1m;
            for (var scale = 0; scale <= 28; scale++)
            {
                factors.Add(new BsonDecimal128(new Decimal128(power)));
                if (scale != 28) power /= 10m;
            }
            return Let(chosen, value => Let(Reduce(Field(value, "digits"), D(0),
                (coefficient, word) => Op("$add", Op("$multiply", coefficient, D(1000000000m)), word)), coefficient =>
                    Op("$multiply", Choose(negative, D(-1), D(1)), coefficient,
                        Op("$arrayElemAt", Literal(factors), Field(value, "scale")))));
        }

        internal BsonValue Compile(BsonValue parts, BsonValue count, bool average)
        {
            var maximumUnits = Words(((BigInteger.One << 96) - 1) * BigInteger.Pow(10, 28));
            return Let(parts, input => Let(count, countValue =>
                Choose(Op("$and", Op(average ? "$gt" : "$gte", countValue, D(0)),
                    Op("$lte", countValue, Literal(CosmicNumericScalarResults.MaximumParticipatingValues))),
                    Let(Normalize(input), signed => Let(Op("$lt", Field(signed, "carry"), D(0)), negative =>
                    Let(Choose(negative,
                        Normalize(Reduce(input, new BsonArray(), (state, word) =>
                            Op("$concatArrays", state, new BsonArray { Op("$multiply", word, D(-1)) }))), signed), magnitude =>
                    Let(Op("$concatArrays", new BsonArray { Field(magnitude, "carry") }, Field(magnitude, "digits")), words =>
                        Choose(average ? Literal(false) : Greater(words, maximumUnits), Literal(BsonNull.Value),
                            Let(average ? (BsonValue)new BsonDocument("$toDecimal", countValue) : D(1), denominator =>
                            Let(Divide(words, denominator), divided =>
                            Let(Round(Field(divided, "q"), Field(divided, "r"), denominator), result =>
                                Choose(Field(result, "done"), Publish(Field(result, "chosen"), negative), Literal(BsonNull.Value)))))))))),
                    Literal(BsonNull.Value))));
        }
    }
}
