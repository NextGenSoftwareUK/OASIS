using System;
using System.Numerics;
using MongoDB.Bson;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    private static BsonValue SingleFloorInteger(BsonValue value)
        => new BsonDocument("$toLong", new BsonDocument("$floor", value));

    // Exact powers, base10^8, least-significant limb first. Multiplication by
    // the25-bit midpoint coefficient fits Int64; no binary floating arithmetic.
    private static BsonArray SingleDecimalFactors()
    {
        var factors = new BsonArray();
        for (var exponent = -150; exponent <= 103; exponent++)
        {
            var power = BigInteger.Pow(exponent < 0 ? 5 : 2, Math.Abs(exponent));
            var limbs = new BsonArray();
            do { limbs.Add(new BsonInt64((long)(power % 100000000))); power /= 100000000; } while (power != 0);
            factors.Add(limbs);
        }
        return factors;
    }

    private static BsonValue SinglePaddedLimb(BsonValue limb)
        => SingleLet("cqlmidpad", Op("$concat", Literal("00000000"), new BsonDocument("$toString", limb)),
            Op("$substrBytes", "$$cqlmidpad", Op("$subtract", new BsonDocument("$strLenBytes", "$$cqlmidpad"), Literal(8)), Literal(8)));

    // Return the exact midpoint's lower34-significant-digit Decimal128 and
    // whether that prefix is the whole value. A Decimal128 input equal to an
    // inexact prefix is BELOW the midpoint, not a tie.
    private static BsonValue SingleDecimalMidpoint(BsonValue bits, BsonArray factors)
    {
        var coefficient = Op("$add", Op("$multiply", Literal(2L),
            Op("$add", Op("$mod", bits, Literal(8388608L)),
                Choose(Op("$eq", "$$cqlmidbinaryexponent", Literal(0L)), Literal(0L), Literal(8388608L)))), Literal(1L));
        var product = Op("$add", Op("$multiply", "$$this", "$$cqlmidcoefficient"), "$$value.carry");
        var carry = SingleFloorInteger(Op("$divide", new BsonDocument("$toDecimal", "$$cqlmidlimbproduct"),
            Literal(new BsonDecimal128(100000000m))));
        var multiply = new BsonDocument("$reduce", new BsonDocument
        {
            { "input", Op("$arrayElemAt", Literal(factors), Op("$add", "$$cqlmidexponent", Literal(150L))) },
            { "initialValue", Literal(new BsonDocument { { "carry", 0L }, { "limbs", new BsonArray() } }) },
            { "in", SingleLet("cqlmidlimbproduct", product, new BsonDocument
                {
                    { "carry", carry },
                    { "limbs", Op("$concatArrays", "$$value.limbs",
                        new BsonArray { Op("$mod", "$$cqlmidlimbproduct", Literal(100000000L)) }) }
                }) }
        });
        var digits = new BsonDocument("$reduce", new BsonDocument
        {
            { "input", new BsonDocument("$reverseArray", Op("$concatArrays", "$$cqlmidproduct.limbs",
                new BsonArray { "$$cqlmidproduct.carry" })) },
            { "initialValue", Literal("") }, { "in", Op("$concat", "$$value", SinglePaddedLimb("$$this")) }
        });
        var prefix = Op("$substrBytes", "$$cqlmiddigits", Literal(0), "$$cqlmidkeep");
        var tail = Op("$substrBytes", "$$cqlmiddigits", "$$cqlmidkeep", Op("$subtract", "$$cqlmidlength", "$$cqlmidkeep"));
        var scale = Op("$add", Choose(Op("$lt", "$$cqlmidexponent", Literal(0L)), "$$cqlmidexponent", Literal(0L)),
            Op("$subtract", "$$cqlmidlength", "$$cqlmidkeep"));
        var boundary = new BsonDocument
        {
            { "floor", new BsonDocument("$toDecimal", Op("$concat", prefix, Literal("E"), new BsonDocument("$toString", scale))) },
            { "exact", Op("$eq", new BsonDocument("$trim", new BsonDocument { { "input", tail }, { "chars", Literal("0") } }), Literal("")) }
        };
        return SingleLet("cqlmidbinaryexponent", SingleFloorInteger(Op("$divide", bits, Literal(8388608L))),
            SingleLet("cqlmidexponent", Choose(Op("$eq", "$$cqlmidbinaryexponent", Literal(0L)), Literal(-150L),
                Op("$subtract", "$$cqlmidbinaryexponent", Literal(151L))),
                SingleLet("cqlmidcoefficient", coefficient,
                    SingleLet("cqlmidproduct", multiply,
                        SingleLet("cqlmiddigits", new BsonDocument("$ltrim", new BsonDocument { { "input", digits }, { "chars", Literal("0") } }),
                            SingleLet("cqlmidlength", new BsonDocument("$strLenBytes", "$$cqlmiddigits"),
                                SingleLet("cqlmidkeep", Choose(Op("$gt", "$$cqlmidlength", Literal(34)), Literal(34), "$$cqlmidlength"), boundary)))))));
    }

    private static BsonValue SingleValueFromPositiveBits(BsonValue bits)
    {
        var powers = new BsonArray();
        for (var exponent = 0; exponent <= 254; exponent++) powers.Add(new BsonDouble(Math.Pow(2, exponent == 0 ? -149 : exponent - 150)));
        return SingleLet("cqldecodeexponent", SingleFloorInteger(Op("$divide", bits, Literal(8388608L))),
            Op("$multiply", Op("$add", Op("$mod", bits, Literal(8388608L)),
                Choose(Op("$eq", "$$cqldecodeexponent", Literal(0L)), Literal(0L), Literal(8388608L))),
                Op("$arrayElemAt", Literal(powers), "$$cqldecodeexponent")));
    }

    internal static BsonValue SingleFromNativeDecimal(BsonValue input)
    {
        var invalid = Literal(new BsonDocument());
        var boundary = SingleDecimalMidpoint("$$cqldecimalmid", SingleDecimalFactors());
        var above = SingleLet("cqldecimalboundary", boundary,
            Op("$or", Op("$gt", "$$cqldecimalabs", "$$cqldecimalboundary.floor"),
                Op("$and", Op("$eq", "$$cqldecimalabs", "$$cqldecimalboundary.floor"), "$$cqldecimalboundary.exact",
                    Op("$eq", Op("$mod", "$$cqldecimalmid", Literal(2L)), Literal(1L)))));
        var step = SingleLet("cqldecimallo", "$$value.low", SingleLet("cqldecimalhi", "$$value.high",
            Choose(Op("$lt", "$$cqldecimallo", "$$cqldecimalhi"),
                SingleLet("cqldecimalmid", SingleFloorInteger(Op("$divide", Op("$add", "$$cqldecimallo", "$$cqldecimalhi"), Literal(2L))),
                    Choose(above,
                        new BsonDocument { { "low", Op("$add", "$$cqldecimalmid", Literal(1L)) }, { "high", "$$cqldecimalhi" } },
                        new BsonDocument { { "low", "$$cqldecimallo" }, { "high", "$$cqldecimalmid" } })),
                new BsonDocument { { "low", "$$cqldecimallo" }, { "high", "$$cqldecimalhi" } })));
        var search = new BsonDocument("$reduce", new BsonDocument
        {
            { "input", Op("$range", Literal(0), Literal(31)) },
            { "initialValue", Literal(new BsonDocument { { "low", 0L }, { "high", 0x7f800000L } }) }, { "in", step }
        });
        var value = SingleValueFromPositiveBits("$$cqldecimalfinished.low");
        var publish = SingleLet("cqldecimalfinished", search,
            Choose(Op("$and", Op("$gt", "$$cqldecimalfinished.low", Literal(0L)),
                Op("$lt", "$$cqldecimalfinished.low", Literal(0x7f800000L))),
                Choose(Op("$lt", "$$cqldecimalinput", Literal(new BsonDecimal128(0m))), Op("$multiply", value, Literal(-1d)), value), invalid));
        var normalize = SingleLet("cqldecimalabs", new BsonDocument("$abs", "$$cqldecimalinput"),
            Choose(Op("$eq", "$$cqldecimalabs", Literal(new BsonDecimal128(0m))), Literal(0d),
                Choose(Op("$and", Op("$gte", "$$cqldecimalabs", Literal(new BsonDecimal128(0m))),
                    Op("$lte", "$$cqldecimalabs", Literal(new BsonDecimal128(Decimal128.Parse("3.4028236E38"))))), publish, invalid)));
        return SingleLet("cqldecimalinput", input,
            Choose(Op("$eq", Type("$$cqldecimalinput"), Literal("decimal")), normalize, invalid));
    }
}
