using System;
using MongoDB.Bson;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    private static BsonValue SingleLet(string name, BsonValue value, BsonValue body) => new BsonDocument("$let",
        new BsonDocument { { "vars", new BsonDocument(name, value) }, { "in", body } });

    private static BsonValue RoundSingleUnits(BsonValue units)
    {
        var fraction = Op("$subtract", "$$cqlroundunits", "$$cqlroundfloor");
        var increment = Op("$or", Op("$gt", fraction, Literal(0.5d)),
            Op("$and", Op("$eq", fraction, Literal(0.5d)),
                Op("$eq", Op("$mod", "$$cqlroundfloor", Literal(2d)), Literal(1d))));
        return SingleLet("cqlroundunits", units,
            SingleLet("cqlroundfloor", new BsonDocument("$floor", "$$cqlroundunits"),
                Op("$add", "$$cqlroundfloor", Choose(increment, Literal(1d), Literal(0d)))));
    }

    // Native binary64 -> binary32 component. Decimal128 inputs require their
    // own exact source-domain normalization before Single is enrolled.
    internal static BsonValue SingleFromNativeDouble(BsonValue input)
    {
        var invalid = Literal(new BsonDocument());
        var bins = new BsonArray();
        for (var exponent = -126; exponent <= 127; exponent++)
            bins.Add(new BsonDocument
            {
                { "threshold", new BsonDouble(Math.Pow(2, exponent)) },
                { "quantum", new BsonDouble(Math.Pow(2, exponent - 23)) }
            });

        var quantum = new BsonDocument("$reduce", new BsonDocument
        {
            { "input", Literal(bins) }, { "initialValue", Literal((double)float.Epsilon) },
            { "in", Choose(Op("$gte", "$$cqlsingleabs", "$$this.threshold"), "$$this.quantum", "$$value") }
        });
        // Scaling by a power of two is exact. Split integer/fraction explicitly
        // so no decimal conversion or decimal-place rounding changes a tie.
        var rounded = Op("$multiply", RoundSingleUnits(Op("$divide", "$$cqlsingleabs", "$$cqlsinglequantum")), "$$cqlsinglequantum");
        var publish = Choose(Op("$eq", "$$cqlsinglerounded", Literal(0d)),
            Choose(Op("$eq", "$$cqlsingleinput", Literal(0d)), Literal(0d), invalid),
            Choose(Op("$lt", "$$cqlsingleinput", Literal(0d)),
                Op("$multiply", "$$cqlsinglerounded", Literal(-1d)), "$$cqlsinglerounded"));
        var normalize = SingleLet("cqlsingleabs", new BsonDocument("$abs", "$$cqlsingleinput"),
            SingleLet("cqlsinglequantum", quantum, SingleLet("cqlsinglerounded", rounded, publish)));
        var overflowMidpoint = (double)float.MaxValue + Math.Pow(2, 103);
        var finite = Op("$and", Op("$gt", "$$cqlsingleinput", Literal(-overflowMidpoint)),
            Op("$lt", "$$cqlsingleinput", Literal(overflowMidpoint)));
        return SingleLet("cqlsingleinput", input,
            Choose(Op("$eq", Type("$$cqlsingleinput"), Literal("double")),
                Choose(finite, normalize, invalid), invalid));
    }

    internal static BsonValue SingleFromNativeInteger(BsonValue input)
    {
        var invalid = Literal(new BsonDocument());
        var bins = new BsonArray();
        decimal power = 1m;
        for (var exponent = 0; exponent <= 63; exponent++, power *= 2m)
            if (exponent >= 24)
                bins.Add(new BsonDocument { { "threshold", new BsonDecimal128(power) },
                    { "quantum", new BsonDecimal128(power / 8388608m) } });

        var quantum = new BsonDocument("$reduce", new BsonDocument
        {
            { "input", Literal(bins) }, { "initialValue", Literal(new BsonDecimal128(1m)) },
            { "in", Choose(Op("$gte", "$$cqlintegerabs", "$$this.threshold"), "$$this.quantum", "$$value") }
        });
        // Convert the signed source to Decimal128 BEFORE abs/division. Every
        // Int64 is exact; the largest spacing is2^40. A non-tie differs from a
        // half by at least2^-40, far beyond Decimal128 quotient rounding error.
        // The final rounded integer has <=20digits and is exact before widening.
        var rounded = Op("$multiply", RoundSingleUnits(Op("$divide", "$$cqlintegerabs", "$$cqlintegerquantum")),
            "$$cqlintegerquantum");
        var signed = Choose(Op("$lt", "$$cqlintegerinput", Literal(0L)),
            Op("$multiply", "$$cqlintegerrounded", Literal(-1L)), "$$cqlintegerrounded");
        var normalize = SingleLet("cqlintegerabs", new BsonDocument("$abs", new BsonDocument("$toDecimal", "$$cqlintegerinput")),
            Choose(Op("$lte", "$$cqlintegerabs", Literal(new BsonDecimal128(16777216m))),
                new BsonDocument("$toDouble", "$$cqlintegerinput"),
                SingleLet("cqlintegerquantum", quantum,
                    SingleLet("cqlintegerrounded", rounded, new BsonDocument("$toDouble", signed)))));
        return SingleLet("cqlintegerinput", input,
            Choose(Op("$in", Type("$$cqlintegerinput"), Literal(new BsonArray { "int", "long" })), normalize, invalid));
    }
}
