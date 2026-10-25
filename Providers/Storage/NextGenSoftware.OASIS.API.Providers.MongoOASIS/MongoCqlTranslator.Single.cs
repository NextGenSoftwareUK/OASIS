using System;
using MongoDB.Bson;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    // Native binary64 -> binary32 component. Integer/Decimal128 inputs require
    // their own exact source-domain normalization before Single is enrolled.
    internal static BsonValue SingleFromNativeDouble(BsonValue input)
    {
        BsonValue Let(string name, BsonValue value, BsonValue body) => new BsonDocument("$let",
            new BsonDocument { { "vars", new BsonDocument(name, value) }, { "in", body } });
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
        var floor = new BsonDocument("$floor", "$$cqlsingleunits");
        var fraction = Op("$subtract", "$$cqlsingleunits", "$$cqlsinglefloor");
        var increment = Op("$or", Op("$gt", fraction, Literal(0.5d)),
            Op("$and", Op("$eq", fraction, Literal(0.5d)),
                Op("$eq", Op("$mod", "$$cqlsinglefloor", Literal(2d)), Literal(1d))));
        var rounded = Op("$multiply", Op("$add", "$$cqlsinglefloor",
            Choose(increment, Literal(1d), Literal(0d))), "$$cqlsinglequantum");
        var publish = Choose(Op("$eq", "$$cqlsinglerounded", Literal(0d)),
            Choose(Op("$eq", "$$cqlsingleinput", Literal(0d)), Literal(0d), invalid),
            Choose(Op("$lt", "$$cqlsingleinput", Literal(0d)),
                Op("$multiply", "$$cqlsinglerounded", Literal(-1d)), "$$cqlsinglerounded"));
        var normalize = Let("cqlsingleabs", new BsonDocument("$abs", "$$cqlsingleinput"),
            Let("cqlsinglequantum", quantum,
                Let("cqlsingleunits", Op("$divide", "$$cqlsingleabs", "$$cqlsinglequantum"),
                    Let("cqlsinglefloor", floor, Let("cqlsinglerounded", rounded, publish)))));
        var overflowMidpoint = (double)float.MaxValue + Math.Pow(2, 103);
        var finite = Op("$and", Op("$gt", "$$cqlsingleinput", Literal(-overflowMidpoint)),
            Op("$lt", "$$cqlsingleinput", Literal(overflowMidpoint)));
        return Let("cqlsingleinput", input,
            Choose(Op("$eq", Type("$$cqlsingleinput"), Literal("double")),
                Choose(finite, normalize, invalid), invalid));
    }
}
